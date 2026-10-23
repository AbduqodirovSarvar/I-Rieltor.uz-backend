using Application.Interfaces.Auctions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using static IRieltor.Tests.Integration.AuctionScenario;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-06: what happens after the bidding: who pays, who is offered the property when the winner does not, whose deposit is
    /// kept and whose goes back. Time is moved by handing the lifecycle service a later "now".
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class SettlementTests(ApiFactory api)
    {
        private readonly AuctionScenario _scenario = new(api);
        private static int _phone = 904_000_000;

        private sealed record Sale(string AuctionId, string PropertyId, DateTime FinishedAt, Applicant A, Applicant B, Applicant C);

        private async Task<AuctionLifecycleRun> RunAsync(DateTime now)
        {
            using var scope = api.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IAuctionLifecycleService>().RunOnceAsync(now);
        }

        private async Task<string> Str(string sql) => await api.ScalarAsync<string>(sql) ?? "";
        private Task<string> AuctionStatus(string id) => Str($"select \"Status\" from \"Auctions\" where \"Id\" = '{id}'");
        private Task<string> Outcome(string auctionId) => Str($"select \"Outcome\" from \"AuctionResults\" where \"AuctionId\" = '{auctionId}'");
        private Task<string> PropertyStatus(string id) => Str($"select \"Status\" from \"Properties\" where \"Id\" = '{id}'");
        private Task<string> DepositOf(Applicant a) => Str($"select \"DepositStatus\" from \"AuctionParticipants\" where \"Id\" = '{a.ParticipantId}'");
        private Task<string> AwardStatus(string auctionId, int rank) =>
            Str($"select a.\"Status\" from \"AuctionAwards\" a join \"AuctionResults\" r on r.\"Id\" = a.\"AuctionResultId\" where r.\"AuctionId\" = '{auctionId}' and a.\"Rank\" = {rank}");

        private async Task<long> RefundsPending(Applicant a)
            => await api.ScalarAsync<long>($"select count(*) from \"Payments\" where \"ParticipantId\" = '{a.ParticipantId}' and \"Purpose\" = 'REFUND' and \"Status\" = 'PENDING'");

        private async Task<long> Notifications(Applicant a, string type)
            => await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"UserId\" = '{a.UserId}' and \"Type\" = '{type}'");

        /// <summary>
        /// Three bidders, deposits paid. Bids: A 100, B 101, C 102, A 103 million. So A wins at 103, C is the runner-up (102), B lost.
        /// </summary>
        private async Task<Sale> FinishedSaleAsync(HttpClient admin, bool offerToRunnerUp = true)
        {
            var auction = await _scenario.PublishedAuctionAsync(admin, offerToRunnerUp: offerToRunnerUp);
            var id = auction["id"]!.GetValue<string>();
            var a = await _scenario.ParticipantAsync(admin, id);
            var b = await _scenario.ParticipantAsync(admin, id);
            var c = await _scenario.ParticipantAsync(admin, id);
            foreach (var person in new[] { a, b, c })   // reachable by SMS, so the notifications are queued
                await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '+998{Interlocked.Increment(ref _phone)}' where \"Id\" = '{person.UserId}'");

            await RunAsync(Time(auction["startsAt"]).AddSeconds(1));
            foreach (var (person, amount) in new[] { (a, 100_000_000), (b, 101_000_000), (c, 102_000_000), (a, 103_000_000) })
                await Ok(await person.Client.PostAsJsonAsync("api/bid", new { auctionId = id, amount }));

            var finishedAt = Time(auction["endsAt"]).AddSeconds(1);
            await RunAsync(finishedAt);
            Assert.Equal("FINISHED", await AuctionStatus(id));
            return new Sale(id, auction["propertyId"]!.GetValue<string>(), finishedAt, a, b, c);
        }

        [Fact]
        public async Task The_winner_is_told_what_to_pay_the_losers_get_their_deposit_back_and_the_runner_up_waits()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            Assert.Equal("AWAITING_PAYMENT", await Outcome(sale.AuctionId));
            Assert.Equal("ACCEPTED", await AwardStatus(sale.AuctionId, 1));
            // Refunded: B only. A is the buyer; C's deposit is held in case A does not pay.
            Assert.Equal(1L, await RefundsPending(sale.B));
            Assert.Equal(0L, await RefundsPending(sale.A));
            Assert.Equal(0L, await RefundsPending(sale.C));
            Assert.Equal(5_000_000m, await api.ScalarAsync<decimal>($"select \"Amount\" from \"Payments\" where \"ParticipantId\" = '{sale.B.ParticipantId}' and \"Purpose\" = 'REFUND'"));
            Assert.Equal(1L, await Notifications(sale.A, "AUCTION_WON"));

            // The cabinet: the price, and what is still to pay after the deposit (103 - 5 million).
            var mine = (await Ok(await sale.A.Client.GetAsync("api/participation/my")))["data"]!.AsArray();
            var award = Assert.Single(mine, m => m!["auctionId"]!.GetValue<string>() == sale.AuctionId)!["award"]!;
            Assert.Equal(103_000_000m, award["price"]!.GetValue<decimal>());
            Assert.Equal(98_000_000m, award["amountDue"]!.GetValue<decimal>());
            Assert.Equal("ACCEPTED", award["status"]!.GetValue<string>());
        }

        [Fact]
        public async Task Staff_lists_show_people_and_lots_not_only_ids()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            // The offers of the sale: who each one is for.
            var result = (await Ok(await admin.GetAsync($"api/auction?Id={sale.AuctionId}")))["data"]!["result"]!;
            var awards = result["awards"]!.AsArray();
            Assert.NotEmpty(awards);   // the buyer's offer; the runner-up's comes when the buyer defaults
            Assert.All(awards, a =>
            {
                Assert.False(string.IsNullOrWhiteSpace(a!["userFullName"]?.GetValue<string>()));
                Assert.NotNull(a["participantNumber"]);
                Assert.NotNull(a["userId"]);
            });
            Assert.Equal(sale.A.UserId.ToString(), awards[0]!["userId"]!.GetValue<string>());

            // Payments: the payer and the lot.
            var lot = (await Ok(await admin.GetAsync($"api/auction?Id={sale.AuctionId}")))["data"]!["lotNumber"]!.GetValue<string>();
            var refunds = (await Ok(await admin.GetAsync($"api/payment/list?Filters%5BauctionId.equals%5D={sale.AuctionId}&Filters%5Bpurpose.equals%5D=REFUND")))["data"]!.AsArray();
            var refund = Assert.Single(refunds)!;
            Assert.Equal(lot, refund["lotNumber"]!.GetValue<string>());
            Assert.False(string.IsNullOrWhiteSpace(refund["userFullName"]?.GetValue<string>()));

            // Notifications: the recipient.
            var notices = (await Ok(await admin.GetAsync($"api/notification/list?Filters%5BuserId.equals%5D={sale.A.UserId}&PageSize=50")))["data"]!.AsArray();
            Assert.NotEmpty(notices);
            Assert.All(notices, n => Assert.False(string.IsNullOrWhiteSpace(n!["userFullName"]?.GetValue<string>())));
        }

        [Fact]
        public async Task The_buyer_pays_the_price_less_the_deposit_and_the_property_is_sold_and_the_runner_up_is_released()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            // The exact amount only.
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/payment/manual", new { participantId = sale.A.ParticipantId, purpose = "FINAL_PAYMENT", amount = 103_000_000, provider = "BANK_TRANSFER" })).StatusCode);
            var paid = (await Ok(await admin.PostAsJsonAsync("api/payment/manual",
                new { participantId = sale.A.ParticipantId, purpose = "FINAL_PAYMENT", amount = 98_000_000, provider = "BANK_TRANSFER", reference = "BANK-REF-1", note = "Bank o'tkazmasi" })))["data"]!;
            Assert.Equal("PAID", paid["status"]!.GetValue<string>());

            Assert.Equal("SOLD", await Outcome(sale.AuctionId));
            Assert.Equal("PAID", await AwardStatus(sale.AuctionId, 1));
            Assert.Equal("SOLD", await PropertyStatus(sale.PropertyId));
            Assert.Equal(1L, await RefundsPending(sale.C));   // no longer held
            Assert.Equal(1L, await Notifications(sale.A, "PAYMENT_RECEIVED"));
            // Paid once; a second time is not possible.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/payment/manual", new { participantId = sale.A.ParticipantId, purpose = "FINAL_PAYMENT", amount = 98_000_000, provider = "BANK_TRANSFER" })).StatusCode);
            // The sold property is not in the catalog for auction again.
            Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync($"api/property/public?Id={sale.PropertyId}")).StatusCode);
        }

        [Fact]
        public async Task A_buyer_who_misses_the_deadline_loses_the_deposit_and_the_property_is_offered_to_the_runner_up_at_their_own_bid()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            // Still within the 5 days: nothing happens.
            await RunAsync(sale.FinishedAt.AddDays(4));
            Assert.Equal("AWAITING_PAYMENT", await Outcome(sale.AuctionId));

            var missed = sale.FinishedAt.AddDays(5).AddSeconds(5);
            var run = await RunAsync(missed);
            Assert.True(run.Defaulted >= 1);

            Assert.Equal("DEFAULTED", await AwardStatus(sale.AuctionId, 1));
            Assert.Equal("FORFEITED", await DepositOf(sale.A));
            Assert.Equal(0L, await RefundsPending(sale.A));   // forfeited deposits are not given back
            Assert.Equal(1L, await Notifications(sale.A, "WINNER_DEFAULTED"));
            // The runner-up (C, 102 million) is offered the property at 102, not at the winner's 103.
            Assert.Equal("AWAITING_RUNNER_UP", await Outcome(sale.AuctionId));
            Assert.Equal("OFFERED", await AwardStatus(sale.AuctionId, 2));
            Assert.Equal(102_000_000m, await api.ScalarAsync<decimal>($"select a.\"Price\" from \"AuctionAwards\" a join \"AuctionResults\" r on r.\"Id\" = a.\"AuctionResultId\" where r.\"AuctionId\" = '{sale.AuctionId}' and a.\"Rank\" = 2"));
            Assert.Null(await api.ScalarAsync<Guid?>($"select \"WinnerParticipantId\" from \"AuctionResults\" where \"AuctionId\" = '{sale.AuctionId}'"));   // nobody is the buyer while the offer is open
            Assert.Equal(1L, await Notifications(sale.C, "RUNNER_UP_OFFER"));
            Assert.Equal("IN_AUCTION", await PropertyStatus(sale.PropertyId));

            var offer = Assert.Single((await Ok(await sale.C.Client.GetAsync("api/participation/my")))["data"]!.AsArray(), m => m!["auctionId"]!.GetValue<string>() == sale.AuctionId)!["award"]!;
            Assert.Equal("OFFERED", offer["status"]!.GetValue<string>());
            Assert.Equal(97_000_000m, offer["amountDue"]!.GetValue<decimal>());   // 102 - 5 million deposit
            Assert.InRange(Time(offer["respondBy"]), missed.AddDays(2).AddMinutes(-1), missed.AddDays(2).AddMinutes(1));
        }

        [Fact]
        public async Task The_runner_up_accepts_and_pays_and_the_property_is_sold_at_their_price()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);
            await RunAsync(sale.FinishedAt.AddDays(5).AddSeconds(5));

            // Only the one who was offered can answer.
            Assert.Equal(HttpStatusCode.Conflict, (await sale.B.Client.PostAsJsonAsync("api/participation/offer/accept", new { auctionId = sale.AuctionId })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await sale.C.Client.PostAsJsonAsync("api/participation/offer/accept", new { auctionId = sale.AuctionId })).StatusCode);

            Assert.Equal("AWAITING_PAYMENT", await Outcome(sale.AuctionId));
            Assert.Equal("ACCEPTED", await AwardStatus(sale.AuctionId, 2));
            Assert.Equal(sale.C.ParticipantId, (await api.ScalarAsync<Guid>($"select \"WinnerParticipantId\" from \"AuctionResults\" where \"AuctionId\" = '{sale.AuctionId}'")).ToString());
            Assert.Equal(102_000_000m, await api.ScalarAsync<decimal>($"select \"FinalPrice\" from \"AuctionResults\" where \"AuctionId\" = '{sale.AuctionId}'"));

            await Ok(await admin.PostAsJsonAsync("api/payment/manual", new { participantId = sale.C.ParticipantId, purpose = "FINAL_PAYMENT", amount = 97_000_000, provider = "BANK_TRANSFER" }));
            Assert.Equal("SOLD", await Outcome(sale.AuctionId));
            Assert.Equal("SOLD", await PropertyStatus(sale.PropertyId));
            Assert.Equal("FORFEITED", await DepositOf(sale.A));
        }

        [Fact]
        public async Task A_runner_up_who_accepts_and_does_not_pay_forfeits_the_deposit_and_the_sale_ends_unsold()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);
            var defaultedAt = sale.FinishedAt.AddDays(5).AddSeconds(5);
            await RunAsync(defaultedAt);
            await sale.C.Client.PostAsJsonAsync("api/participation/offer/accept", new { auctionId = sale.AuctionId });

            // The second payment deadline counts from the acceptance (the real clock), so move far past it.
            await RunAsync(DateTime.UtcNow.AddDays(30));

            Assert.Equal("DEFAULTED", await AwardStatus(sale.AuctionId, 2));
            Assert.Equal("FORFEITED", await DepositOf(sale.C));
            Assert.Equal("NOT_SOLD", await Outcome(sale.AuctionId));
            Assert.Equal("PUBLISHED", await PropertyStatus(sale.PropertyId));
            // Third place is not offered the property.
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuctionAwards\" a join \"AuctionResults\" r on r.\"Id\" = a.\"AuctionResultId\" where r.\"AuctionId\" = '{sale.AuctionId}' and a.\"Rank\" > 2"));
        }

        [Fact]
        public async Task A_runner_up_who_declines_or_stays_silent_gets_the_deposit_back_and_the_sale_ends_unsold()
        {
            var admin = await api.AdminClientAsync();

            var declined = await FinishedSaleAsync(admin);
            await RunAsync(declined.FinishedAt.AddDays(5).AddSeconds(5));
            Assert.Equal(HttpStatusCode.OK, (await declined.C.Client.PostAsJsonAsync("api/participation/offer/decline", new { auctionId = declined.AuctionId })).StatusCode);
            Assert.Equal("DECLINED", await AwardStatus(declined.AuctionId, 2));
            Assert.Equal("NOT_SOLD", await Outcome(declined.AuctionId));
            Assert.Equal(1L, await RefundsPending(declined.C));
            Assert.Equal("PUBLISHED", await PropertyStatus(declined.PropertyId));
            // The property can be put up again.
            await _scenario.PublishedAuctionAsync(admin, propertyId: declined.PropertyId);

            var silent = await FinishedSaleAsync(admin);
            var offeredAt = silent.FinishedAt.AddDays(5).AddSeconds(5);
            await RunAsync(offeredAt);
            Assert.Equal("OFFERED", await AwardStatus(silent.AuctionId, 2));
            var run = await RunAsync(offeredAt.AddDays(2).AddMinutes(1));
            Assert.True(run.OffersExpired >= 1);
            Assert.Equal("EXPIRED", await AwardStatus(silent.AuctionId, 2));
            Assert.Equal("NOT_SOLD", await Outcome(silent.AuctionId));
            Assert.Equal(1L, await RefundsPending(silent.C));
            // Too late to answer an offer that has lapsed.
            Assert.Equal(HttpStatusCode.Conflict, (await silent.C.Client.PostAsJsonAsync("api/participation/offer/accept", new { auctionId = silent.AuctionId })).StatusCode);
        }

        [Fact]
        public async Task Without_the_runner_up_option_everybody_but_the_buyer_is_refunded_at_once_and_a_default_ends_the_sale()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin, offerToRunnerUp: false);

            Assert.Equal(1L, await RefundsPending(sale.B));
            Assert.Equal(1L, await RefundsPending(sale.C));   // not held: nobody will be offered the property
            await RunAsync(sale.FinishedAt.AddDays(5).AddSeconds(5));

            Assert.Equal("NOT_SOLD", await Outcome(sale.AuctionId));
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuctionAwards\" a join \"AuctionResults\" r on r.\"Id\" = a.\"AuctionResultId\" where r.\"AuctionId\" = '{sale.AuctionId}' and a.\"Rank\" = 2"));
            Assert.Equal("PUBLISHED", await PropertyStatus(sale.PropertyId));
        }

        [Fact]
        public async Task A_buyer_can_give_up_and_it_counts_as_not_paying_and_nobody_else_can_refuse_for_them()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            Assert.Equal(HttpStatusCode.Conflict, (await sale.C.Client.PostAsJsonAsync("api/participation/refuse", new { auctionId = sale.AuctionId })).StatusCode);   // the runner-up is not the buyer
            Assert.Equal(HttpStatusCode.OK, (await sale.A.Client.PostAsJsonAsync("api/participation/refuse", new { auctionId = sale.AuctionId })).StatusCode);

            Assert.Equal("DEFAULTED", await AwardStatus(sale.AuctionId, 1));
            Assert.Equal("FORFEITED", await DepositOf(sale.A));
            Assert.Equal("OFFERED", await AwardStatus(sale.AuctionId, 2));
            Assert.Equal(HttpStatusCode.Conflict, (await sale.A.Client.PostAsJsonAsync("api/participation/refuse", new { auctionId = sale.AuctionId })).StatusCode);
        }

        [Fact]
        public async Task The_buyer_is_reminded_once_before_the_deadline()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);

            await RunAsync(sale.FinishedAt.AddDays(2));
            Assert.Equal(0L, await Notifications(sale.A, "PAYMENT_DUE_SOON"));
            var run = await RunAsync(sale.FinishedAt.AddDays(4).AddHours(12));   // 12 hours before the deadline
            Assert.True(run.PaymentsReminded >= 1);
            await RunAsync(sale.FinishedAt.AddDays(4).AddHours(18));
            Assert.Equal(1L, await Notifications(sale.A, "PAYMENT_DUE_SOON"));
        }

        [Fact]
        public async Task A_sale_without_bids_returns_every_deposit_and_a_cancelled_auction_tells_everyone_and_returns_the_paid_ones()
        {
            var admin = await api.AdminClientAsync();

            var quiet = await _scenario.PublishedAuctionAsync(admin);
            var quietId = quiet["id"]!.GetValue<string>();
            var one = await _scenario.ParticipantAsync(admin, quietId);
            var two = await _scenario.ParticipantAsync(admin, quietId);
            await RunAsync(Time(quiet["endsAt"]).AddSeconds(1));   // started and ended with no bid
            Assert.Equal("FAILED", await AuctionStatus(quietId));
            Assert.Equal(1L, await RefundsPending(one));
            Assert.Equal(1L, await RefundsPending(two));

            var cancelled = await _scenario.PublishedAuctionAsync(admin);
            var cancelledId = cancelled["id"]!.GetValue<string>();
            var paid = await _scenario.ParticipantAsync(admin, cancelledId, depositPaid: true);
            var unpaid = await _scenario.ParticipantAsync(admin, cancelledId, depositPaid: false);
            var pending = await _scenario.ApplyAsync(cancelledId);
            foreach (var person in new[] { paid, unpaid, pending })
                await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '+998{Interlocked.Increment(ref _phone)}' where \"Id\" = '{person.UserId}'");

            await Ok(await admin.PostAsJsonAsync("api/auction/cancel", new { id = cancelledId, reason = "Sud qarori" }));

            Assert.Equal(1L, await RefundsPending(paid));
            Assert.Equal(0L, await RefundsPending(unpaid));
            Assert.Equal(0L, await RefundsPending(pending));
            foreach (var person in new[] { paid, unpaid, pending })
                Assert.Equal(1L, await Notifications(person, "AUCTION_CANCELLED"));
        }

        [Fact]
        public async Task A_completed_refund_marks_the_deposit_returned_once_and_tells_the_participant()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);
            var refund = await api.ScalarAsync<Guid>($"select \"Id\" from \"Payments\" where \"ParticipantId\" = '{sale.B.ParticipantId}' and \"Purpose\" = 'REFUND'");

            var list = (await Ok(await admin.GetAsync("api/payment/list?Filters%5Bpurpose.equals%5D=REFUND&Filters%5Bstatus.equals%5D=PENDING&PageSize=100")))["data"]!.AsArray();
            Assert.Contains(list, p => p!["id"]!.GetValue<string>() == refund.ToString());

            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("api/payment/refund/complete", new { id = refund, reference = "BANK-OUT-9" })).StatusCode);
            Assert.Equal("REFUNDED", await DepositOf(sale.B));
            Assert.Equal(1L, await Notifications(sale.B, "DEPOSIT_REFUNDED"));
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/payment/refund/complete", new { id = refund })).StatusCode);

            await api.SignUpAsync("settle_plain");
            var plain = api.Authorized(await api.SignInAsync("settle_plain", "Strong@123"));
            Assert.Equal(HttpStatusCode.Forbidden, (await plain.PostAsJsonAsync("api/payment/refund/complete", new { id = refund })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await plain.GetAsync("api/payment/list")).StatusCode);
        }

        [Fact]
        public async Task Money_that_arrives_after_the_buyer_has_defaulted_is_sent_back()
        {
            var admin = await api.AdminClientAsync();
            var sale = await FinishedSaleAsync(admin);
            var checkout = (await Ok(await sale.A.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId = sale.AuctionId, purpose = "FINAL_PAYMENT", provider = "BANK_TRANSFER" })))["data"]!;
            var paymentId = checkout["paymentId"]!.GetValue<string>();
            Assert.Equal(98_000_000m, checkout["amount"]!.GetValue<decimal>());

            await RunAsync(sale.FinishedAt.AddDays(5).AddSeconds(5));   // A defaults before the transfer is confirmed
            await Ok(await admin.PostAsJsonAsync("api/payment/confirm", new { id = paymentId, reference = "LATE-1" }));

            Assert.Equal("DEFAULTED", await AwardStatus(sale.AuctionId, 1));   // nothing was settled
            Assert.NotEqual("SOLD", await Outcome(sale.AuctionId));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"Payments\" where \"Purpose\" = 'REFUND' and \"Note\" = 'Qaytarish: to''lov {paymentId}' and \"Amount\" = 98000000"));
        }
    }
}

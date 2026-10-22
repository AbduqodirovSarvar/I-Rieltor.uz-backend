using Application.Interfaces.Auctions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static IRieltor.Tests.Integration.AuctionScenario;

namespace IRieltor.Tests.Integration
{
    /// <summary>P2-05: bidding rules, simultaneous bids, extension of the sale, and what watchers receive live.</summary>
    [Collection(ApiCollection.Name)]
    public class BidTests(ApiFactory api)
    {
        private readonly AuctionScenario _scenario = new(api);
        private static int _phone = 903_000_000;

        private async Task RunAsync(DateTime now)
        {
            using var scope = api.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IAuctionLifecycleService>().RunOnceAsync(now);
        }

        /// <summary>An ONGOING auction with the given number of participants (approved, deposit paid).</summary>
        private async Task<(string AuctionId, DateTime StartsAt, DateTime EndsAt, List<AuctionScenario.Applicant> Bidders)> OngoingAsync(HttpClient admin, int bidders, int extensionMinutes = 0)
        {
            var auction = await _scenario.PublishedAuctionAsync(admin, extensionMinutes: extensionMinutes);
            var id = auction["id"]!.GetValue<string>();
            var participants = new List<AuctionScenario.Applicant>();
            for (var i = 0; i < bidders; i++)
                participants.Add(await _scenario.ParticipantAsync(admin, id));
            var startsAt = Time(auction["startsAt"]);
            await RunAsync(startsAt.AddSeconds(1));
            Assert.Equal("ONGOING", await api.ScalarAsync<string>($"select \"Status\" from \"Auctions\" where \"Id\" = '{id}'"));
            return (id, startsAt, Time(auction["endsAt"]), participants);
        }

        private static Task<HttpResponseMessage> BidAsync(HttpClient client, string auctionId, decimal amount)
            => client.PostAsJsonAsync("api/bid", new { auctionId, amount });

        [Fact]
        public async Task Only_approved_participants_with_a_paid_deposit_bid_and_only_while_the_sale_is_open()
        {
            var admin = await api.AdminClientAsync();
            var auction = await _scenario.PublishedAuctionAsync(admin);
            var id = auction["id"]!.GetValue<string>();
            var unpaid = await _scenario.ParticipantAsync(admin, id, depositPaid: false);
            var pending = await _scenario.ApplyAsync(id);
            var paid = await _scenario.ParticipantAsync(admin, id);
            var outsider = await api.EimzoUserAsync();

            // Not started yet.
            Assert.Equal(HttpStatusCode.Conflict, (await BidAsync(paid.Client, id, 100_000_000m)).StatusCode);
            await RunAsync(Time(auction["startsAt"]).AddSeconds(1));   // also closes the pending application

            Assert.Equal(HttpStatusCode.Unauthorized, (await BidAsync(api.CreateClient(), id, 100_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await BidAsync(outsider.Client, id, 100_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await BidAsync(pending.Client, id, 100_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await BidAsync(unpaid.Client, id, 100_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await BidAsync(paid.Client, Guid.NewGuid().ToString(), 100_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await BidAsync(paid.Client, id, 100_000_000m)).StatusCode);
        }

        [Fact]
        public async Task The_amount_must_reach_the_starting_price_then_rise_by_the_step_and_a_leader_cannot_outbid_themself()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, _, bidders) = await OngoingAsync(admin, 2);
            var (first, second) = (bidders[0], bidders[1]);

            Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(first.Client, id, 99_999_999m)).StatusCode);
            var placed = (await Ok(await BidAsync(first.Client, id, 100_000_000m)))["data"]!;   // the starting price itself is allowed
            Assert.Equal(100_000_000m, placed["currentPrice"]!.GetValue<decimal>());
            Assert.Equal(101_000_000m, placed["minNextBid"]!.GetValue<decimal>());
            Assert.Equal(1, placed["participantNumber"]!.GetValue<int>());
            Assert.False(placed["extended"]!.GetValue<bool>());

            Assert.Equal(HttpStatusCode.Conflict, (await BidAsync(first.Client, id, 105_000_000m)).StatusCode);     // already leading
            var tooLow = await BidAsync(second.Client, id, 100_500_000m);
            Assert.Equal(HttpStatusCode.BadRequest, tooLow.StatusCode);
            Assert.Contains("101 000 000", (await ApiFactory.JsonAsync(tooLow))["message"]!.GetValue<string>());
            Assert.Equal(HttpStatusCode.OK, (await BidAsync(second.Client, id, 101_000_000m)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(first.Client, id, 0)).StatusCode);

            var page = (await Ok(await api.CreateClient().GetAsync($"api/auction/public?Id={id}")))["data"]!;
            Assert.Equal(101_000_000m, page["currentPrice"]!.GetValue<decimal>());
            Assert.Equal(102_000_000m, page["minNextBid"]!.GetValue<decimal>());
            Assert.Equal(2, page["bidCount"]!.GetValue<int>());
        }

        [Fact]
        public async Task The_public_history_shows_participant_numbers_and_amounts_only()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, _, bidders) = await OngoingAsync(admin, 2);
            await BidAsync(bidders[0].Client, id, 100_000_000m);
            await BidAsync(bidders[1].Client, id, 101_000_000m);
            await BidAsync(bidders[0].Client, id, 102_000_000m);

            var body = await Ok(await api.CreateClient().GetAsync($"api/bid/list?AuctionId={id}"));
            var list = body["data"]!.AsArray();
            Assert.Equal([102_000_000m, 101_000_000m, 100_000_000m], list.Select(b => b!["amount"]!.GetValue<decimal>()));
            Assert.Equal([1, 2, 1], list.Select(b => b!["participantNumber"]!.GetValue<int>()));
            Assert.Equal(3, body["totalCount"]!.GetValue<int>());
            Assert.All(list, b => Assert.Equal(["amount", "participantNumber", "placedAt"], b!.AsObject().Select(p => p.Key).Order()));
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"api/bid/list?AuctionId={Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task Bids_at_the_same_moment_never_both_win_and_the_price_is_the_highest_accepted_bid()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, _, bidders) = await OngoingAsync(admin, 6);

            // The same amount from everybody at once: exactly one gets it.
            var same = await Task.WhenAll(bidders.Select(b => BidAsync(b.Client, id, 100_000_000m)));
            Assert.Equal(1, same.Count(r => r.StatusCode == HttpStatusCode.OK));
            // The others either collided with the winner (409) or arrived after it and found the price already higher (400).
            Assert.All(same.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.True(r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest, r.StatusCode.ToString()));

            // Different amounts at once: whatever was accepted is consistent with what is stored.
            var next = await Task.WhenAll(bidders.Select((b, i) => BidAsync(b.Client, id, 110_000_000m + i * 1_000_000m)));
            var accepted = next.Count(r => r.StatusCode == HttpStatusCode.OK);
            Assert.InRange(accepted, 1, bidders.Count);
            Assert.All(next.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.True(r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest, r.StatusCode.ToString()));

            Assert.Equal(1L + accepted, await api.ScalarAsync<long>($"select count(*) from \"Bids\" where \"AuctionId\" = '{id}'"));
            Assert.Equal(await api.ScalarAsync<decimal>($"select max(\"Amount\") from \"Bids\" where \"AuctionId\" = '{id}'"),
                         await api.ScalarAsync<decimal>($"select \"CurrentPrice\" from \"Auctions\" where \"Id\" = '{id}'"));
            Assert.Equal(await api.ScalarAsync<Guid>($"select \"Id\" from \"Bids\" where \"AuctionId\" = '{id}' order by \"Amount\" desc limit 1"),
                         await api.ScalarAsync<Guid>($"select \"LeadingBidId\" from \"Auctions\" where \"Id\" = '{id}'"));
        }

        [Fact]
        public async Task A_late_bid_extends_the_sale_and_a_bid_after_the_end_is_refused()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, _, bidders) = await OngoingAsync(admin, 3, extensionMinutes: 5);
            // Three minutes left, an hour ago started.
            await api.ScalarAsync<object>($"update \"Auctions\" set \"StartsAt\" = now() - interval '1 hour', \"ApplicationDeadline\" = now() - interval '2 hours', \"EndsAt\" = now() + interval '3 minutes' where \"Id\" = '{id}'");

            var late = (await Ok(await BidAsync(bidders[0].Client, id, 100_000_000m)))["data"]!;
            Assert.True(late["extended"]!.GetValue<bool>());
            Assert.InRange(Time(late["endsAt"]), DateTime.UtcNow.AddMinutes(4.5), DateTime.UtcNow.AddMinutes(5.5));

            // Plenty of time left: no extension.
            await api.ScalarAsync<object>($"update \"Auctions\" set \"EndsAt\" = now() + interval '1 hour' where \"Id\" = '{id}'");
            Assert.False((await Ok(await BidAsync(bidders[1].Client, id, 101_000_000m)))["data"]!["extended"]!.GetValue<bool>());

            // The end has passed (the clock has not finished the sale yet).
            await api.ScalarAsync<object>($"update \"Auctions\" set \"EndsAt\" = now() - interval '1 second' where \"Id\" = '{id}'");
            Assert.Equal(HttpStatusCode.Conflict, (await BidAsync(bidders[2].Client, id, 102_000_000m)).StatusCode);
        }

        [Fact]
        public async Task The_leader_who_is_outbid_is_told_by_email_but_not_by_sms()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, _, bidders) = await OngoingAsync(admin, 2);
            var email = $"outbid_{bidders[0].UserId:N}@tests.local";
            var phone = "+998" + Interlocked.Increment(ref _phone);
            await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '{phone}', \"Email\" = '{email}', \"EmailConfirmedAt\" = now() where \"Id\" = '{bidders[0].UserId}'");

            await BidAsync(bidders[0].Client, id, 100_000_000m);
            await BidAsync(bidders[1].Client, id, 103_000_000m);
            using (var scope = api.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<Application.Interfaces.Notifications.INotificationDispatcher>().DispatchDueAsync(DateTime.UtcNow.AddSeconds(2));

            var mail = Assert.Single(api.Sinks.EmailsTo(email).Where(e => e.Subject == "Taklifingiz oshirildi"));
            Assert.Contains("103 000 000", mail.Html);
            Assert.Empty(api.Sinks.SmsTo(phone));
        }

        private HubConnection Hub() => new HubConnectionBuilder()
            .WithUrl(new Uri(api.Server.BaseAddress, "hubs/auction"), options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
            .Build();

        [Fact]
        public async Task Watchers_get_bids_and_status_changes_of_their_auction_live_and_nothing_else()
        {
            var admin = await api.AdminClientAsync();
            var (id, _, endsAt, bidders) = await OngoingAsync(admin, 2);
            var (otherId, _, _, _) = await OngoingAsync(admin, 1);

            var bids = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var statuses = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var strangers = 0;
            await using var watcher = Hub();
            watcher.On<JsonElement>("BidPlaced", e => { if (e.GetProperty("auctionId").GetGuid().ToString() == id) bids.TrySetResult(e); else Interlocked.Increment(ref strangers); });
            watcher.On<JsonElement>("AuctionStatusChanged", e => { if (e.GetProperty("auctionId").GetGuid().ToString() == id) statuses.TrySetResult(e); else Interlocked.Increment(ref strangers); });
            await watcher.StartAsync();
            await watcher.InvokeAsync("Join", Guid.Parse(id));

            await BidAsync(bidders[0].Client, otherId, 100_000_000m);   // another auction: not for this watcher (they are no participant there either)
            await BidAsync(bidders[0].Client, id, 100_000_000m);
            var bid = await bids.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(100_000_000m, bid.GetProperty("amount").GetDecimal());
            Assert.Equal(1, bid.GetProperty("participantNumber").GetInt32());
            Assert.Equal(101_000_000m, bid.GetProperty("minNextBid").GetDecimal());
            Assert.Equal(1, bid.GetProperty("bidCount").GetInt32());

            await RunAsync(endsAt.AddSeconds(1));
            var status = await statuses.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("FINISHED", status.GetProperty("status").GetString());
            Assert.Equal(0, strangers);

            // Leaving, and the cap on watched auctions.
            await watcher.InvokeAsync("Leave", Guid.Parse(id));
            for (var i = 0; i < 20; i++)
                await watcher.InvokeAsync("Join", Guid.NewGuid());
            await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => watcher.InvokeAsync("Join", Guid.NewGuid()));
        }
    }
}

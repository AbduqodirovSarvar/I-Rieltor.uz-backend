using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Persistence.Data;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-01: the rules the database itself keeps (CHECK and unique constraints), whatever the services do.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class AuctionDomainTests(ApiFactory api)
    {
        private async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> action)
        {
            using var scope = api.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        private Task SaveAsync(params object[] entities) => WithContextAsync(async context =>
        {
            context.AddRange(entities);
            return await context.SaveChangesAsync();
        });

        private static string ConstraintOf(DbUpdateException ex) => (ex.InnerException as PostgresException)?.ConstraintName ?? ex.Message;

        private static readonly DateTime Start = new(2026, 12, 1, 5, 0, 0, DateTimeKind.Utc);

        private static Auction NewAuction(Guid propertyId, string lot) => new()
        {
            LotNumber = lot, Title = "Lot " + lot, PropertyId = propertyId,
            StartingPrice = 100_000_000m, MinBidStep = 1_000_000m, DepositAmount = 5_000_000m,
            ApplicationDeadline = Start.AddDays(-1), StartsAt = Start, EndsAt = Start.AddHours(2),
        };

        private async Task<(Property Property, Auction Auction, AuctionParticipant A, AuctionParticipant B)> SeedAsync(string key)
        {
            var category = new PropertyCategory { Name = "Kvartira " + key };
            var property = new Property { Title = "3 xonali", CategoryId = category.Id, Region = "Toshkent", Address = "Chilonzor 1", AreaSqm = 72.5m };
            var auction = NewAuction(property.Id, "L-" + key);
            await api.SignUpAsync("bidder_a_" + key);
            await api.SignUpAsync("bidder_b_" + key);
            var a = new AuctionParticipant { AuctionId = auction.Id, UserId = await api.ScalarAsync<Guid>($"select \"Id\" from \"Users\" where \"Login\" = 'bidder_a_{key}'"), Status = ParticipantStatus.APPROVED, ParticipantNumber = 1 };
            var b = new AuctionParticipant { AuctionId = auction.Id, UserId = await api.ScalarAsync<Guid>($"select \"Id\" from \"Users\" where \"Login\" = 'bidder_b_{key}'"), Status = ParticipantStatus.APPROVED, ParticipantNumber = 2 };
            await SaveAsync(category, property, auction, a, b);
            return (property, auction, a, b);
        }

        [Fact]
        public async Task A_full_auction_is_stored_with_statuses_as_text_and_money_exact()
        {
            var (_, auction, a, _) = await SeedAsync("ok");
            var bid = new Bid { AuctionId = auction.Id, ParticipantId = a.Id, Amount = 101_000_000.55m };
            await SaveAsync(bid);
            await SaveAsync(new AuctionResult { AuctionId = auction.Id, Outcome = AuctionOutcome.SOLD, WinnerParticipantId = a.Id, WinningBidId = bid.Id, FinalPrice = bid.Amount },
                            new Payment { UserId = a.UserId, AuctionId = auction.Id, ParticipantId = a.Id, Purpose = PaymentPurpose.DEPOSIT, Amount = 5_000_000m, Provider = PaymentProvider.CLICK, ProviderTransactionId = "click-1", Status = PaymentStatus.PAID, PaidAt = DateTime.UtcNow });

            Assert.Equal("DRAFT", await api.ScalarAsync<string>($"select \"Status\" from \"Auctions\" where \"Id\" = '{auction.Id}'"));
            Assert.Equal("APPROVED", await api.ScalarAsync<string>($"select \"Status\" from \"AuctionParticipants\" where \"Id\" = '{a.Id}'"));
            Assert.Equal(101_000_000.55m, await api.ScalarAsync<decimal>($"select \"Amount\" from \"Bids\" where \"Id\" = '{bid.Id}'"));
        }

        [Fact]
        public async Task Two_bids_of_the_same_amount_in_one_auction_are_refused()
        {
            var (_, auction, a, b) = await SeedAsync("dup");
            await SaveAsync(new Bid { AuctionId = auction.Id, ParticipantId = a.Id, Amount = 102_000_000m });

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(new Bid { AuctionId = auction.Id, ParticipantId = b.Id, Amount = 102_000_000m }));
            Assert.Equal("IX_Bids_AuctionId_Amount", ConstraintOf(ex));
        }

        [Fact]
        public async Task A_property_has_one_live_auction_but_may_be_auctioned_again_after_a_failed_one()
        {
            var (property, auction, _, _) = await SeedAsync("live");

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(NewAuction(property.Id, "L-live-2")));
            Assert.Equal("IX_Auctions_PropertyId_Live", ConstraintOf(ex));

            await api.ScalarAsync<object>($"update \"Auctions\" set \"Status\" = 'FAILED' where \"Id\" = '{auction.Id}'");
            await SaveAsync(NewAuction(property.Id, "L-live-3"));
        }

        [Theory]
        [InlineData("times")]
        [InlineData("price")]
        [InlineData("step")]
        [InlineData("deadline")]
        public async Task Impossible_auction_settings_are_refused(string what)
        {
            var category = new PropertyCategory { Name = "Uy " + what };
            var property = new Property { Title = "Uy", CategoryId = category.Id, Region = "Samarqand", Address = "Markaz" };
            await SaveAsync(category, property);
            var auction = NewAuction(property.Id, "L-bad-" + what);
            switch (what)
            {
                case "times": auction.EndsAt = auction.StartsAt.AddMinutes(-1); break;
                case "price": auction.StartingPrice = 0; break;
                case "step": auction.MinBidStep = -5; break;
                case "deadline": auction.ApplicationDeadline = auction.StartsAt.AddHours(1); break;
            }

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(auction));
            Assert.StartsWith("CK_Auctions_", ConstraintOf(ex));
        }

        [Fact]
        public async Task A_sold_result_needs_a_winner_and_a_repeated_payment_callback_is_refused()
        {
            var (_, auction, a, _) = await SeedAsync("res");

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(new AuctionResult { AuctionId = auction.Id, Outcome = AuctionOutcome.SOLD }));
            Assert.Equal("CK_AuctionResults_Winner", ConstraintOf(ex));

            Payment Deposit() => new() { UserId = a.UserId, AuctionId = auction.Id, ParticipantId = a.Id, Purpose = PaymentPurpose.DEPOSIT, Amount = 5_000_000m, Provider = PaymentProvider.PAYME, ProviderTransactionId = "payme-77" };
            await SaveAsync(Deposit());
            ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(Deposit()));
            Assert.Equal("IX_Payments_Provider_ProviderTransactionId", ConstraintOf(ex));
        }

        [Fact]
        public async Task When_the_winner_defaults_the_property_goes_to_the_runner_up_at_its_own_bid()
        {
            var (_, auction, a, b) = await SeedAsync("runner");
            var bidB = new Bid { AuctionId = auction.Id, ParticipantId = b.Id, Amount = 103_000_000m };
            var bidA = new Bid { AuctionId = auction.Id, ParticipantId = a.Id, Amount = 104_000_000m };
            await SaveAsync(bidB);
            await SaveAsync(bidA);
            var result = new AuctionResult { AuctionId = auction.Id, Outcome = AuctionOutcome.AWAITING_PAYMENT, WinnerParticipantId = a.Id, WinningBidId = bidA.Id, FinalPrice = bidA.Amount };
            var winner = new AuctionAward { AuctionResultId = result.Id, Rank = 1, ParticipantId = a.Id, BidId = bidA.Id, Price = bidA.Amount, Status = AwardStatus.ACCEPTED };
            await SaveAsync(result, winner);

            // While the winner's award is live, nobody else can be offered the property.
            AuctionAward RunnerUp() => new() { AuctionResultId = result.Id, Rank = 2, ParticipantId = b.Id, BidId = bidB.Id, Price = bidB.Amount, Status = AwardStatus.OFFERED };
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(RunnerUp()));
            Assert.Equal("IX_AuctionAwards_AuctionResultId_Live", ConstraintOf(ex));

            // The winner cannot simply "decline" like the runner-up: refusing is a default.
            var pg = await Assert.ThrowsAsync<PostgresException>(() =>
                api.ScalarAsync<object>($"update \"AuctionAwards\" set \"Status\" = 'DECLINED' where \"Id\" = '{winner.Id}'"));
            Assert.Equal("CK_AuctionAwards_WinnerBound", pg.ConstraintName);

            await api.ScalarAsync<object>($"update \"AuctionAwards\" set \"Status\" = 'DEFAULTED', \"ResolvedAt\" = now() where \"Id\" = '{winner.Id}'");
            await api.ScalarAsync<object>($"update \"AuctionParticipants\" set \"DepositStatus\" = 'FORFEITED' where \"Id\" = '{a.Id}'");
            await SaveAsync(RunnerUp());
            await api.ScalarAsync<object>($"update \"AuctionResults\" set \"WinnerParticipantId\" = '{b.Id}', \"WinningBidId\" = '{bidB.Id}', \"FinalPrice\" = {bidB.Amount} where \"Id\" = '{result.Id}'");

            Assert.Equal(103_000_000m, await api.ScalarAsync<decimal>($"select \"FinalPrice\" from \"AuctionResults\" where \"Id\" = '{result.Id}'"));
            Assert.Equal("FORFEITED", await api.ScalarAsync<string>($"select \"DepositStatus\" from \"AuctionParticipants\" where \"Id\" = '{a.Id}'"));

            // Only two places.
            await api.SignUpAsync("bidder_c_runner");
            var c = new AuctionParticipant { AuctionId = auction.Id, UserId = await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'bidder_c_runner'"), Status = ParticipantStatus.APPROVED, ParticipantNumber = 3 };
            var bidC = new Bid { AuctionId = auction.Id, ParticipantId = c.Id, Amount = 102_000_000m };
            await SaveAsync(c);
            await SaveAsync(bidC);
            await api.ScalarAsync<object>($"update \"AuctionAwards\" set \"Status\" = 'DECLINED' where \"Rank\" = 2 and \"AuctionResultId\" = '{result.Id}'");
            ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(new AuctionAward { AuctionResultId = result.Id, Rank = 3, ParticipantId = c.Id, BidId = bidC.Id, Price = bidC.Amount, Status = AwardStatus.OFFERED }));
            Assert.Equal("CK_AuctionAwards_Rank", ConstraintOf(ex));
        }

        [Fact]
        public async Task A_user_applies_once_per_auction_and_category_names_ignore_case()
        {
            var (_, auction, a, _) = await SeedAsync("once");

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(new AuctionParticipant { AuctionId = auction.Id, UserId = a.UserId }));
            Assert.Equal("IX_AuctionParticipants_AuctionId_UserId", ConstraintOf(ex));

            ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(new PropertyCategory { Name = "KVARTIRA ONCE" }));
            Assert.Equal("IX_PropertyCategories_Name", ConstraintOf(ex));
        }
    }
}

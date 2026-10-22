using Application.Interfaces.Auctions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistance.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-03: the admin side of an auction and the clock-driven steps. Time is moved by passing our own "now" to
    /// <see cref="IAuctionLifecycleService.RunOnceAsync"/>; the background timer is off in the tests.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class AuctionLifecycleTests(ApiFactory api)
    {
        private static MultipartFormDataContent Form(params (string Name, string Value)[] fields)
        {
            var form = new MultipartFormDataContent();
            foreach (var (name, value) in fields)
                form.Add(new StringContent(value), name);
            return form;
        }

        private static async Task<JsonObject> Ok(HttpResponseMessage response)
        {
            var body = await ApiFactory.JsonAsync(response);
            Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
            return body;
        }

        private static string Iso(DateTime utc) => utc.ToString("O", CultureInfo.InvariantCulture);

        private static DateTime Time(JsonNode? node) => DateTime.Parse(node!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        private async Task<string> CreatePropertyAsync(HttpClient admin, string status = "PUBLISHED")
        {
            var key = Guid.NewGuid().ToString("N")[..8];
            var category = (await Ok(await admin.PostAsync("api/propertycategory", Form(("Name", "Toifa " + key)))))["data"]!["id"]!.GetValue<string>();
            return (await Ok(await admin.PostAsync("api/property", Form(("Title", "Obyekt " + key), ("CategoryId", category), ("Region", "Hudud-" + key), ("Address", "Manzil 1"), ("Status", status)))))["data"]!["id"]!.GetValue<string>();
        }

        private static (string, string)[] AuctionFields(string propertyId, DateTime start, string? title = null)
            => [("PropertyId", propertyId), ("Title", title ?? ""), ("StartingPrice", "100000000"), ("MinBidStep", "1000000"), ("DepositAmount", "5000000"),
                ("ApplicationDeadline", Iso(start.AddHours(-1))), ("StartsAt", Iso(start)), ("EndsAt", Iso(start.AddHours(2))), ("ExtensionMinutes", "5")];

        private async Task<JsonObject> CreateAuctionAsync(HttpClient admin, string propertyId, DateTime? start = null)
            => (await Ok(await admin.PostAsync("api/auction", Form(AuctionFields(propertyId, start ?? DateTime.UtcNow.AddDays(1))))))["data"]!.AsObject();

        private Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object body) => client.PostAsJsonAsync(url, body);

        private async Task<AuctionLifecycleRun> RunAsync(DateTime now)
        {
            using var scope = api.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IAuctionLifecycleService>().RunOnceAsync(now);
        }

        private async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> action)
        {
            using var scope = api.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        private async Task<string> StatusAsync(string auctionId) => await api.ScalarAsync<string>($"select \"Status\" from \"Auctions\" where \"Id\" = '{auctionId}'") ?? "";
        private async Task<string> PropertyStatusAsync(string propertyId) => await api.ScalarAsync<string>($"select \"Status\" from \"Properties\" where \"Id\" = '{propertyId}'") ?? "";

        private async Task<AuctionParticipant> AddParticipantAsync(string auctionId, string login, ParticipantStatus status, int number)
        {
            await api.SignUpAsync(login);
            var participant = new AuctionParticipant
            {
                AuctionId = Guid.Parse(auctionId),
                UserId = await api.ScalarAsync<Guid>($"select \"Id\" from \"Users\" where \"Login\" = '{login}'"),
                Status = status,
                ParticipantNumber = status == ParticipantStatus.APPROVED ? number : null,
            };
            await WithContextAsync(async c => { c.Add(participant); return await c.SaveChangesAsync(); });
            return participant;
        }

        private async Task<Bid> AddBidAsync(string auctionId, AuctionParticipant participant, decimal amount)
        {
            var bid = new Bid { AuctionId = Guid.Parse(auctionId), ParticipantId = participant.Id, Amount = amount };
            await WithContextAsync(async c => { c.Add(bid); return await c.SaveChangesAsync(); });
            return bid;
        }

        [Fact]
        public async Task A_draft_is_checked_changed_published_and_then_locked()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var start = DateTime.UtcNow.AddDays(1);

            // Mistakes in the times are refused.
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form(AuctionFields(property, DateTime.UtcNow.AddHours(-1))))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form([.. AuctionFields(property, start).Where(f => f.Item1 != "EndsAt"), ("EndsAt", Iso(start.AddMinutes(2)))]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form([.. AuctionFields(property, start).Where(f => f.Item1 != "ApplicationDeadline"), ("ApplicationDeadline", Iso(start.AddMinutes(1)))]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form([.. AuctionFields(property, start).Where(f => f.Item1 != "StartingPrice"), ("StartingPrice", "0")]))).StatusCode);
            // Without "Z" or an offset it is unclear whether it is Tashkent or UTC.
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form([.. AuctionFields(property, start).Where(f => f.Item1 != "StartsAt"), ("StartsAt", start.ToString("yyyy-MM-ddTHH:mm:ss"))]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/auction", Form([.. AuctionFields(Guid.NewGuid().ToString(), start)]))).StatusCode);

            var created = await CreateAuctionAsync(admin, property, start);
            var id = created["id"]!.GetValue<string>();
            Assert.Equal("DRAFT", created["status"]!.GetValue<string>());
            Assert.Matches(@"^\d{4}-\d{6}$", created["lotNumber"]!.GetValue<string>());
            Assert.StartsWith("Obyekt ", created["title"]!.GetValue<string>());   // taken from the property

            // One live auction per property.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync("api/auction", Form(AuctionFields(property, start)))).StatusCode);
            // A draft is invisible to visitors.
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"api/auction/public?Id={id}")).StatusCode);

            var updated = await Ok(await admin.PutAsync("api/auction", Form([("Id", id), .. AuctionFields(property, start, title: "Yangi nom")])));
            Assert.Equal("Yangi nom", updated["data"]!["title"]!.GetValue<string>());
            Assert.Equal(created["lotNumber"]!.GetValue<string>(), updated["data"]!["lotNumber"]!.GetValue<string>());

            var published = await Ok(await PostJsonAsync(admin, "api/auction/publish", new { id }));
            Assert.Equal("SCHEDULED", published["data"]!["status"]!.GetValue<string>());
            Assert.Equal("IN_AUCTION", await PropertyStatusAsync(property));
            Assert.Equal(HttpStatusCode.Conflict, (await PostJsonAsync(admin, "api/auction/publish", new { id })).StatusCode);

            // Locked: only cancelling is left.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsync("api/auction", Form([("Id", id), .. AuctionFields(property, start)]))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/auction") { Content = JsonContent.Create(new { id }) })).StatusCode);
            // The property's own status follows the auction.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsync("api/property", Form(("Id", property), ("Title", "x"), ("CategoryId", Guid.NewGuid().ToString()), ("Region", "r"), ("Address", "a"), ("Status", "PUBLISHED")))).StatusCode);

            var page = (await Ok(await api.CreateClient().GetAsync($"api/auction/public?Id={id}")))["data"]!;
            Assert.Equal("SCHEDULED", page["status"]!.GetValue<string>());
            Assert.Equal(0, page["participantCount"]!.GetValue<int>());
            Assert.Equal(100_000_000m, page["startingPrice"]!.GetValue<decimal>());
            var inCatalog = (await Ok(await api.CreateClient().GetAsync($"api/property/public?Id={property}")))["data"]!;
            Assert.Equal(id, inCatalog["auction"]!["id"]!.GetValue<string>());
        }

        [Fact]
        public async Task A_draft_can_be_deleted_and_its_property_auctioned_again()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var id = (await CreateAuctionAsync(admin, property))["id"]!.GetValue<string>();

            Assert.Equal(HttpStatusCode.OK, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/auction") { Content = JsonContent.Create(new { id }) })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"api/auction?Id={id}")).StatusCode);
            await CreateAuctionAsync(admin, property);
        }

        [Fact]
        public async Task A_draft_property_cannot_be_published_into_an_auction()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin, status: "DRAFT");
            var id = (await CreateAuctionAsync(admin, property))["id"]!.GetValue<string>();

            Assert.Equal(HttpStatusCode.BadRequest, (await PostJsonAsync(admin, "api/auction/publish", new { id })).StatusCode);
            Assert.Equal("DRAFT", await StatusAsync(id));
        }

        [Fact]
        public async Task The_highest_bid_of_an_approved_participant_wins_when_the_sale_ends()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var auction = await CreateAuctionAsync(admin, property);
            var id = auction["id"]!.GetValue<string>();
            await Ok(await PostJsonAsync(admin, "api/auction/publish", new { id }));
            var key = id[^6..];
            var a = await AddParticipantAsync(id, "win_a_" + key, ParticipantStatus.APPROVED, 1);
            var b = await AddParticipantAsync(id, "win_b_" + key, ParticipantStatus.APPROVED, 2);
            var rejected = await AddParticipantAsync(id, "win_c_" + key, ParticipantStatus.REJECTED, 3);
            await AddBidAsync(id, a, 101_000_000m);
            var best = await AddBidAsync(id, b, 103_000_000m);
            await AddBidAsync(id, a, 102_000_000m);
            await AddBidAsync(id, rejected, 150_000_000m);   // higher, but not an approved participant

            var startsAt = Time(auction["startsAt"]);
            var endsAt = Time(auction["endsAt"]);

            // Before the start nothing happens.
            await RunAsync(startsAt.AddMinutes(-1));
            Assert.Equal("SCHEDULED", await StatusAsync(id));

            // (The database is shared with the other tests, whose auctions may start in the same run.)
            var started = await RunAsync(startsAt.AddSeconds(1));
            Assert.True(started.Started >= 1);
            Assert.Equal("ONGOING", await StatusAsync(id));

            // During the sale there is no result yet.
            await RunAsync(endsAt.AddMinutes(-1));
            Assert.Equal("ONGOING", await StatusAsync(id));
            var now = endsAt.AddSeconds(1);
            Assert.True((await RunAsync(now)).Finished >= 1);

            Assert.Equal("FINISHED", await StatusAsync(id));
            Assert.Equal("IN_AUCTION", await PropertyStatusAsync(property));   // until it is paid for
            var result = (await Ok(await admin.GetAsync($"api/auction?Id={id}")))["data"]!["result"]!;
            Assert.Equal("AWAITING_PAYMENT", result["outcome"]!.GetValue<string>());
            Assert.Equal(b.Id.ToString(), result["winnerParticipantId"]!.GetValue<string>());
            Assert.Equal(best.Id.ToString(), result["winningBidId"]!.GetValue<string>());
            Assert.Equal(103_000_000m, result["finalPrice"]!.GetValue<decimal>());
            Assert.InRange(Time(result["paymentDueAt"]), now.AddDays(5).AddSeconds(-5), now.AddDays(5).AddSeconds(5));
            var award = Assert.Single(result["awards"]!.AsArray())!;
            Assert.Equal(1, award["rank"]!.GetValue<int>());
            Assert.Equal("ACCEPTED", award["status"]!.GetValue<string>());
            Assert.Equal(103_000_000m, award["price"]!.GetValue<decimal>());

            var page = (await Ok(await api.CreateClient().GetAsync($"api/auction/public?Id={id}")))["data"]!;
            Assert.Equal("AWAITING_PAYMENT", page["outcome"]!.GetValue<string>());
            Assert.Equal(2, page["participantCount"]!.GetValue<int>());
            Assert.Equal(4, page["bidCount"]!.GetValue<int>());
            Assert.Null(page["winnerParticipantId"]);   // visitors do not learn who won

            // Running again changes nothing for this auction.
            await RunAsync(now.AddHours(1));
            Assert.Equal("FINISHED", await StatusAsync(id));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"AuctionResults\" where \"AuctionId\" = '{id}'"));
        }

        [Theory]
        [InlineData(false, "NO_PARTICIPANTS")]
        [InlineData(true, "NO_BIDS")]
        public async Task A_sale_without_participants_or_bids_fails_and_the_property_can_be_auctioned_again(bool withParticipant, string outcome)
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var auction = await CreateAuctionAsync(admin, property);
            var id = auction["id"]!.GetValue<string>();
            await Ok(await PostJsonAsync(admin, "api/auction/publish", new { id }));
            if (withParticipant)
                await AddParticipantAsync(id, "nobid_" + id[^6..], ParticipantStatus.APPROVED, 1);

            // The service was down for the whole sale: one run starts and finishes it.
            var run = await RunAsync(Time(auction["endsAt"]).AddMinutes(10));
            Assert.True(run.Started >= 1 && run.Finished >= 1);

            Assert.Equal("FAILED", await StatusAsync(id));
            Assert.Equal("PUBLISHED", await PropertyStatusAsync(property));
            var result = (await Ok(await admin.GetAsync($"api/auction?Id={id}")))["data"]!["result"]!;
            Assert.Equal(outcome, result["outcome"]!.GetValue<string>());
            Assert.Null(result["winnerParticipantId"]);
            Assert.Empty(result["awards"]!.AsArray());
            await CreateAuctionAsync(admin, property);   // a new try
        }

        [Fact]
        public async Task A_cancelled_auction_returns_its_property_and_is_not_finished_by_the_clock()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var auction = await CreateAuctionAsync(admin, property);
            var id = auction["id"]!.GetValue<string>();

            Assert.Equal(HttpStatusCode.Conflict, (await PostJsonAsync(admin, "api/auction/cancel", new { id, reason = "x" })).StatusCode);   // a draft is deleted
            await Ok(await PostJsonAsync(admin, "api/auction/publish", new { id }));
            Assert.Equal(HttpStatusCode.BadRequest, (await PostJsonAsync(admin, "api/auction/cancel", new { id, reason = "" })).StatusCode);
            await RunAsync(Time(auction["startsAt"]).AddMinutes(1));
            Assert.Equal("ONGOING", await StatusAsync(id));

            var cancelled = await Ok(await PostJsonAsync(admin, "api/auction/cancel", new { id, reason = "  Sud qarori  " }));
            Assert.Equal("CANCELLED", cancelled["data"]!["status"]!.GetValue<string>());
            Assert.Equal("Sud qarori", cancelled["data"]!["cancelReason"]!.GetValue<string>());
            Assert.Equal("PUBLISHED", await PropertyStatusAsync(property));

            await RunAsync(Time(auction["endsAt"]).AddDays(1));
            Assert.Equal("CANCELLED", await StatusAsync(id));
            Assert.Equal(HttpStatusCode.Conflict, (await PostJsonAsync(admin, "api/auction/cancel", new { id, reason = "again" })).StatusCode);
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuctionResults\" where \"AuctionId\" = '{id}'"));
        }

        [Fact]
        public async Task Two_instances_finishing_the_same_auction_produce_one_result()
        {
            var admin = await api.AdminClientAsync();
            var property = await CreatePropertyAsync(admin);
            var auction = await CreateAuctionAsync(admin, property);
            var id = auction["id"]!.GetValue<string>();
            await Ok(await PostJsonAsync(admin, "api/auction/publish", new { id }));
            var p = await AddParticipantAsync(id, "race_" + id[^6..], ParticipantStatus.APPROVED, 1);
            await AddBidAsync(id, p, 120_000_000m);
            var now = Time(auction["endsAt"]).AddMinutes(1);

            var runs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunAsync(now)));

            // Starting and finishing are each done once, however many instances run (the counts cover other tests' auctions too).
            Assert.True(runs.Sum(r => r.Finished) >= 1);
            Assert.Equal("FINISHED", await StatusAsync(id));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"AuctionResults\" where \"AuctionId\" = '{id}'"));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"AuctionAwards\" a join \"AuctionResults\" r on r.\"Id\" = a.\"AuctionResultId\" where r.\"AuctionId\" = '{id}'"));
        }

        [Fact]
        public async Task Admin_endpoints_need_the_permissions_and_the_public_page_is_open()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/auction/list")).StatusCode);

            await api.SignUpAsync("plainuser_auct");
            var user = api.Authorized(await api.SignInAsync("plainuser_auct", "Strong@123"));
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("api/auction/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync(user, "api/auction/publish", new { id = Guid.NewGuid() })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"api/auction/public?Id={Guid.NewGuid()}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateClient().GetAsync($"api/auction/public?Id={Guid.Empty}")).StatusCode);
        }
    }
}

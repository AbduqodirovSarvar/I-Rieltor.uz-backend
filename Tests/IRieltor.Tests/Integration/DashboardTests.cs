using Application.Interfaces.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using static IRieltor.Tests.Integration.AuctionScenario;

namespace IRieltor.Tests.Integration
{
    /// <summary>P2-11: the admin dashboard totals.</summary>
    [Collection(ApiCollection.Name)]
    public class DashboardTests(ApiFactory api)
    {
        private readonly AuctionScenario _scenario = new(api);

        [Fact]
        public async Task Only_staff_with_the_permission_can_read_the_summary()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/dashboard/summary")).StatusCode);
            var user = await api.EimzoUserAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await user.Client.GetAsync("api/dashboard/summary")).StatusCode);

            var admin = await api.AdminClientAsync();
            var data = (await Ok(await admin.GetAsync("api/dashboard/summary")))["data"]!;
            Assert.Equal(30, data["last30Days"]!.AsArray().Count);
            Assert.True(data["users"]!["total"]!.GetValue<int>() >= 1);
            Assert.NotNull(data["payments"]);
            Assert.NotNull(data["sales"]);
        }

        [Fact]
        public async Task The_reader_counts_the_ongoing_and_upcoming_auctions_applications_and_todays_activity()
        {
            var admin = await api.AdminClientAsync();
            var upcoming = await _scenario.PublishedAuctionAsync(admin, start: DateTime.UtcNow.AddDays(400));   // far enough that no lifecycle run of another test starts it
            var running = await _scenario.PublishedAuctionAsync(admin);
            var participant = await _scenario.ParticipantAsync(admin, running["id"]!.GetValue<string>());
            await _scenario.ApplyAsync(upcoming["id"]!.GetValue<string>());   // a pending application

            using var scope = api.Services.CreateScope();
            var lifecycle = scope.ServiceProvider.GetRequiredService<Application.Interfaces.Auctions.IAuctionLifecycleService>();
            await lifecycle.RunOnceAsync(Time(running["startsAt"]).AddSeconds(1));
            await Ok(await participant.Client.PostAsJsonAsync("api/bid", new { auctionId = running["id"]!.GetValue<string>(), amount = 110_000_000 }));

            var summary = await scope.ServiceProvider.GetRequiredService<IDashboardReader>().ReadAsync(DateTime.UtcNow, CancellationToken.None, listSize: 10_000);

            Assert.Contains(summary.Ongoing, a => a.Id == Guid.Parse(running["id"]!.GetValue<string>()) && a.Bids >= 1 && a.Participants >= 1 && a.CurrentPrice == 110_000_000m);
            Assert.Contains(summary.Upcoming, a => a.Id == Guid.Parse(upcoming["id"]!.GetValue<string>()));
            Assert.True(summary.PendingApplications >= 1);
            Assert.True(summary.AuctionsByStatus.GetValueOrDefault("ONGOING") >= 1);
            Assert.True(summary.AuctionsByStatus.GetValueOrDefault("SCHEDULED") >= 1);
            Assert.True(summary.PropertiesByStatus.GetValueOrDefault("IN_AUCTION") >= 2);
            var today = summary.Last30Days[^1];
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), today.Date);
            Assert.True(today.Bids >= 1);
            Assert.True(today.NewUsers >= 1);
            Assert.Equal(30, summary.Last30Days.Count);
            Assert.True(summary.Last30Days.Zip(summary.Last30Days.Skip(1)).All(p => p.First.Date.AddDays(1) == p.Second.Date));
        }
    }
}

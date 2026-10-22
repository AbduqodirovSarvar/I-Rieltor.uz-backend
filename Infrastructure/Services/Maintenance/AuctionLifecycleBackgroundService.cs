using Application.Interfaces.Auctions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Maintenance
{
    /// <summary>
    /// Calls <see cref="IAuctionLifecycleService"/> every <c>Auctions:LifecycleIntervalSeconds</c> (default 10, 0 = off):
    /// the delay between the end of a sale and its result, or between the start time and the first bid, is at most that long.
    /// Migrations have run before the host starts, so there is no start delay.
    /// </summary>
    public sealed class AuctionLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AuctionLifecycleBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var seconds = configuration.GetValue("Auctions:LifecycleIntervalSeconds", 10);
            if (seconds <= 0)
                return;

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
                do
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        await scope.ServiceProvider.GetRequiredService<IAuctionLifecycleService>().RunOnceAsync(DateTime.UtcNow, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Auction lifecycle run failed; it runs again at the next interval");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
        }
    }
}

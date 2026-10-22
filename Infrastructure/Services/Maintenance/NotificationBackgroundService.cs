using Application.Interfaces.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Maintenance
{
    /// <summary>
    /// Sends the queued notifications every <c>Notifications:DispatchIntervalSeconds</c> (default 5, 0 = off).
    /// </summary>
    public sealed class NotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<NotificationBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var seconds = configuration.GetValue("Notifications:DispatchIntervalSeconds", 5);
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
                        await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().DispatchDueAsync(DateTime.UtcNow, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Notification dispatch failed; it runs again at the next interval");
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

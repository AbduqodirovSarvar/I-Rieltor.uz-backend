using Application.Exceptions;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.Notifications
{
    /// <summary>
    /// Sends the queued notifications. Every message has its own scope and transaction-free save: the attempt is
    /// "claimed" first (attempts + 1, next attempt pushed 5 minutes ahead, saved with the row version), so a crash
    /// during sending only delays the retry, and two instances never send the same message.
    /// </summary>
    public sealed class NotificationDispatcher(IServiceScopeFactory scopeFactory, ILogger<NotificationDispatcher> logger) : INotificationDispatcher
    {
        private const int BatchSize = 50;

        public async Task<int> DispatchDueAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            List<Guid> due;
            using (var scope = scopeFactory.CreateScope())
            {
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                due = await notifications.Query(n => n.Status == NotificationStatus.PENDING && n.NextAttemptAt <= now, cancellationToken)
                                         .OrderBy(n => n.NextAttemptAt).Take(BatchSize).Select(n => n.Id).ToListAsync(cancellationToken);
            }

            var sent = 0;
            foreach (var id in due)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    if (await scope.ServiceProvider.GetRequiredService<NotificationSender>().SendAsync(id, now, cancellationToken))
                        sent++;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another instance took this message.
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Notification {NotificationId} could not be processed", id);
                }
            }
            return sent;
        }
    }

    /// <summary>One message; scoped.</summary>
    public sealed class NotificationSender(
        INotificationRepository notifications,
        IEmailService emailService,
        ISmsSender smsSender,
        IUserRepository users,
        ILogger<NotificationSender> logger)
    {
        public const int MaxAttempts = 5;
        private static readonly TimeSpan ClaimFor = TimeSpan.FromMinutes(5);
        // Wait after the 1st, 2nd, 3rd, 4th failed attempt.
        private static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2)];

        public async Task<bool> SendAsync(Guid id, DateTime now, CancellationToken cancellationToken)
        {
            var notification = await notifications.GetAsync(n => n.Id == id, cancellationToken);
            if (notification is not { Status: NotificationStatus.PENDING } || notification.NextAttemptAt > now)
                return false;

            // Claim: if this save loses the race (row version), DbUpdateConcurrencyException leaves the message to the winner.
            notification.Attempts++;
            notification.NextAttemptAt = now.Add(ClaimFor);
            await notifications.UpdateAsync(notification, cancellationToken);

            try
            {
                if (notification.Channel == NotificationChannel.EMAIL)
                {
                    var name = (await users.GetAsync(u => u.Id == notification.UserId, cancellationToken))?.FullName ?? string.Empty;
                    var html = HtmlEmailBody.FromTemplate(NotificationEmailTemplate.Html, new Dictionary<string, string?> { ["name"] = name, ["message"] = notification.Body });
                    await emailService.SendEmailAsync(notification.Destination, notification.Subject ?? "I-Rieltor", html, cancellationToken);
                }
                else if (!smsSender.IsEnabled)
                {
                    notification.Status = NotificationStatus.SKIPPED;
                    notification.LastError = "SMS is not configured.";
                    await notifications.UpdateAsync(notification, cancellationToken);
                    return false;
                }
                else
                {
                    await smsSender.SendAsync(notification.Destination, notification.Body, cancellationToken);
                }

                notification.Status = NotificationStatus.SENT;
                notification.SentAt = now;
                notification.LastError = null;
                await notifications.UpdateAsync(notification, cancellationToken);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A wrong address will not get better; a provider that is down will.
                var permanent = ex is BadRequestException || notification.Attempts >= MaxAttempts;
                notification.LastError = Truncate(ex.Message);
                if (permanent)
                    notification.Status = NotificationStatus.FAILED;
                else
                    notification.NextAttemptAt = now.Add(Backoff[Math.Min(notification.Attempts, Backoff.Length) - 1]);
                await notifications.UpdateAsync(notification, cancellationToken);
                logger.LogWarning(ex, "Notification {NotificationId} ({Channel}) attempt {Attempt} failed{Final}",
                    notification.Id, notification.Channel, notification.Attempts, permanent ? " for good" : "");
                return false;
            }
        }

        private static string Truncate(string message) => message.Length <= 480 ? message : message[..480];
    }
}

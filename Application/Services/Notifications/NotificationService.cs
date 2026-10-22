using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Notifications;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Notifications
{
    public sealed class NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        ISmsSender smsSender) : INotificationService
    {
        public async Task<int> NotifyAsync(Guid userId, NotificationType type, IReadOnlyDictionary<string, string> data, Guid? auctionId = null, bool once = false, CancellationToken cancellationToken = default)
        {
            if (once && await notifications.AnyAsync(n => n.UserId == userId && n.Type == type && n.AuctionId == auctionId, cancellationToken))
                return 0;

            var user = await users.GetAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return 0;

            var template = NotificationTemplates.Get(type);
            var text = NotificationTemplates.Render(template.Text, data);
            var queued = 0;

            // An unconfirmed address is not ours to write to: anyone can type any email into their profile.
            if (!string.IsNullOrWhiteSpace(user.Email) && user.EmailConfirmedAt != null)
            {
                await notifications.CreateAsync(Create(user, NotificationChannel.EMAIL, type, user.Email, template.Subject, text, auctionId), cancellationToken);
                queued++;
            }
            if (template.Sms && smsSender.IsEnabled && !string.IsNullOrWhiteSpace(user.Phone))
            {
                await notifications.CreateAsync(Create(user, NotificationChannel.SMS, type, user.Phone, null, text, auctionId), cancellationToken);
                queued++;
            }
            return queued;
        }

        private static Notification Create(User user, NotificationChannel channel, NotificationType type, string destination, string? subject, string body, Guid? auctionId)
            => new()
            {
                UserId = user.Id,
                Channel = channel,
                Type = type,
                Destination = destination,
                Subject = subject,
                Body = body,
                AuctionId = auctionId,
                NextAttemptAt = DateTime.UtcNow,
            };
    }
}

using Domain.Enums;

namespace Application.Interfaces.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Queues the notification for the user on every channel they can be reached on (a confirmed email; a phone number
        /// when SMS is configured). Call it inside the transaction of the event that causes it: the notification is saved or
        /// lost together with it, and the background dispatcher sends it afterwards.
        /// </summary>
        /// <param name="once">
        /// Skip when the user already has a notification of this type for this auction (reminders that a periodic job
        /// would otherwise repeat).
        /// </param>
        /// <returns>How many messages were queued (0 when the user cannot be reached or <paramref name="once"/> skipped it).</returns>
        Task<int> NotifyAsync(Guid userId, NotificationType type, IReadOnlyDictionary<string, string> data, Guid? auctionId = null, bool once = false, CancellationToken cancellationToken = default);
    }

    public interface INotificationDispatcher
    {
        /// <summary>
        /// Sends what is due at <paramref name="now"/> (UTC). A failure is retried later (1 min, 5 min, 30 min, 2 h), then marked FAILED.
        /// </summary>
        /// <returns>Messages sent in this run.</returns>
        Task<int> DispatchDueAsync(DateTime now, CancellationToken cancellationToken = default);
    }
}

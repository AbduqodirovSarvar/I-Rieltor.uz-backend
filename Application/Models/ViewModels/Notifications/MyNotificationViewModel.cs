using Domain.Enums;

namespace Application.Models.ViewModels.Notifications
{
    /// <summary>One message the signed-in user was sent (an e-mail and an SMS about the same event are shown once).</summary>
    public record MyNotificationViewModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public NotificationType Type { get; set; }
        public string? Subject { get; set; }
        public string Body { get; set; } = string.Empty;
        public Guid? AuctionId { get; set; }
    }
}

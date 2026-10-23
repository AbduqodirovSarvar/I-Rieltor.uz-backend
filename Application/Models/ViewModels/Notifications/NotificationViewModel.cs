using Application.Models.ViewModels.Abstractions;
using Domain.Enums;

namespace Application.Models.ViewModels.Notifications
{
    public record NotificationViewModel : BaseViewModel
    {
        public Guid UserId { get; set; }
        public string? UserFullName { get; set; }
        public NotificationChannel Channel { get; set; }
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Body { get; set; } = string.Empty;
        public Guid? AuctionId { get; set; }
        public int Attempts { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string? LastError { get; set; }
    }
}

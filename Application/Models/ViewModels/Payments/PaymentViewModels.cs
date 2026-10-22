using Application.Models.ViewModels.Abstractions;
using Domain.Enums;

namespace Application.Models.ViewModels.Payments
{
    public record PaymentViewModel : BaseViewModel
    {
        public Guid UserId { get; set; }
        public Guid AuctionId { get; set; }
        public Guid? ParticipantId { get; set; }
        public PaymentPurpose Purpose { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public PaymentProvider Provider { get; set; }
        public string? ProviderTransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>What the payer needs to pay: open <see cref="Url"/> (Payme, Click), or follow <see cref="Instructions"/> (bank transfer).</summary>
    public record CheckoutViewModel
    {
        public Guid PaymentId { get; init; }
        public decimal Amount { get; init; }
        public PaymentPurpose Purpose { get; init; }
        public PaymentProvider Provider { get; init; }
        public string? Url { get; init; }
        public string? Instructions { get; init; }
    }
}

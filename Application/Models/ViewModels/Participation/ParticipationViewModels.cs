using Application.Models.ViewModels.Abstractions;
using Domain.Enums;

namespace Application.Models.ViewModels.Participation
{
    /// <summary>The text the applicant signs with E-IMZO.</summary>
    public record ApplicationTextViewModel(string Text, string TextBase64, DateTime ApplicationDeadline);

    /// <summary>What the applicant sees right after applying.</summary>
    public record ParticipantViewModel : BaseViewModel
    {
        public Guid AuctionId { get; set; }
        public ParticipantStatus Status { get; set; }
        public int? ParticipantNumber { get; set; }
        public DepositStatus DepositStatus { get; set; }
        public string? RejectReason { get; set; }
    }

    /// <summary>One line of the applicant's cabinet: the auction, the application, the deposit and, after the sale, the purchase offer.</summary>
    public record MyParticipationViewModel
    {
        public Guid Id { get; init; }
        public Guid AuctionId { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public string AuctionTitle { get; init; } = string.Empty;
        public AuctionStatus AuctionStatus { get; init; }
        public DateTime StartsAt { get; init; }
        public DateTime EndsAt { get; init; }
        public decimal DepositAmount { get; init; }
        public ParticipantStatus Status { get; init; }
        public int? ParticipantNumber { get; init; }
        public DepositStatus DepositStatus { get; init; }
        public string? RejectReason { get; init; }
        public DateTime CreatedAt { get; init; }
        /// <summary>Set after the sale, when this participant was offered the property (the winner, or the runner-up).</summary>
        public MyAwardViewModel? Award { get; init; }
    }

    public record MyAwardViewModel
    {
        public int Rank { get; init; }
        public AwardStatus Status { get; init; }
        public decimal Price { get; init; }
        /// <summary>What is still to be paid: the price less the deposit (the deposit counts towards the price).</summary>
        public decimal AmountDue { get; init; }
        public DateTime? RespondBy { get; init; }
        public DateTime? PaymentDueAt { get; init; }
    }

    /// <summary>One application as the staff see it.</summary>
    public record ParticipantAdminViewModel : BaseViewModel
    {
        public Guid AuctionId { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        /// <summary>PINFL or INN of the applicant.</summary>
        public string? UserIdentifier { get; set; }
        public string? UserPhone { get; set; }
        public string? UserEmail { get; set; }
        public ParticipantStatus Status { get; set; }
        public int? ParticipantNumber { get; set; }
        public DepositStatus DepositStatus { get; set; }
        public string? SignerIdentifier { get; set; }
        public DateTime? SignedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public string? RejectReason { get; set; }
    }
}

using Application.Models.ViewModels.Abstractions;
using Domain.Enums;

namespace Application.Models.ViewModels.Auctions
{
    public record AuctionViewModel : BaseViewModel
    {
        public string LotNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid PropertyId { get; set; }
        public AuctionStatus Status { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal MinBidStep { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int ExtensionMinutes { get; set; }
        public decimal? CurrentPrice { get; set; }
        public Guid? LeadingBidId { get; set; }
        public bool OfferToRunnerUp { get; set; }
        public string? CancelReason { get; set; }
        /// <summary>Faqat bitta auksionni ochganda to'ldiriladi, va faqat savdo tugagan bo'lsa.</summary>
        public AuctionResultViewModel? Result { get; set; }
        /// <summary>Faqat bitta auksionni ochganda to'ldiriladi (xodim ko'radi).</summary>
        public ParticipantCountsViewModel? ParticipantCounts { get; set; }
    }

    /// <summary>Arizalar soni holati bo'yicha; <c>DepositPaid</c> zakalat to'lagan (hali qaytarilmagan va musodara qilinmagan) ishtirokchilar.</summary>
    public record ParticipantCountsViewModel(int Total, int Pending, int Approved, int Rejected, int Withdrawn, int DepositPaid);

    public record AuctionResultViewModel : BaseViewModel
    {
        public Guid AuctionId { get; set; }
        public AuctionOutcome Outcome { get; set; }
        public Guid? WinnerParticipantId { get; set; }
        public Guid? WinningBidId { get; set; }
        public decimal? FinalPrice { get; set; }
        public DateTime? PaymentDueAt { get; set; }
        public List<AuctionAwardViewModel> Awards { get; set; } = [];
    }

    public record AuctionAwardViewModel : BaseViewModel
    {
        public int Rank { get; set; }
        public Guid ParticipantId { get; set; }
        /// <summary>Who the offer is for (staff see the person; the number is the one in the public bid list).</summary>
        public int? ParticipantNumber { get; set; }
        public Guid? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserPhone { get; set; }
        public Guid BidId { get; set; }
        public decimal Price { get; set; }
        public AwardStatus Status { get; set; }
        public DateTime? RespondBy { get; set; }
        public DateTime? PaymentDueAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}

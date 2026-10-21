using Domain.Abstractions;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Tugagan auksion natijasi (bayonnoma). Har auksionda bittadan.
    /// </summary>
    public class AuctionResult : AudiTableEntity
    {
        /// <summary>
        /// Auksion Id'si (FK). Takrorlanmaydi.
        /// </summary>
        public Guid AuctionId { get; set; }

        /// <summary>
        /// Auksion (navigatsiya).
        /// </summary>
        public Auction? Auction { get; set; }

        /// <summary>
        /// Natija.
        /// </summary>
        public AuctionOutcome Outcome { get; set; }

        /// <summary>
        /// G'olib ishtirokchi Id'si. G'olib bo'lmasa null.
        /// </summary>
        public Guid? WinnerParticipantId { get; set; }

        /// <summary>
        /// G'olib ishtirokchi (navigatsiya).
        /// </summary>
        public AuctionParticipant? WinnerParticipant { get; set; }

        /// <summary>
        /// G'olib taklif Id'si.
        /// </summary>
        public Guid? WinningBidId { get; set; }

        /// <summary>
        /// Yakuniy narx, so'm (g'olib taklif summasi).
        /// </summary>
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// G'olib yakuniy to'lovni shu vaqtgacha qilishi kerak.
        /// </summary>
        public DateTime? PaymentDueAt { get; set; }

        /// <summary>
        /// Bayonnoma fayli Id'si (PDF).
        /// </summary>
        public Guid? ProtocolFileId { get; set; }

        /// <summary>
        /// Bayonnoma fayli (navigatsiya).
        /// </summary>
        public UploadedFile? ProtocolFile { get; set; }
    }
}

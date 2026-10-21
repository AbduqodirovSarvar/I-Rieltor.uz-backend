using Domain.Abstractions;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Tugagan auksion natijasi (bayonnoma). Har auksionda bittadan. Xaridor maydonlari hozirgi (yoki yakuniy)
    /// xaridorni ko'rsatadi: g'olib voz kechib, obyekt ikkinchi o'ringa o'tsa, ular almashadi. Kimga qachon
    /// taklif qilingani tarixi <see cref="Awards"/>da.
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
        /// Xaridor ishtirokchi Id'si (to'lov kutilayotgan yoki to'lagan). Xaridor bo'lmasa null.
        /// </summary>
        public Guid? WinnerParticipantId { get; set; }

        /// <summary>
        /// Xaridor ishtirokchi (navigatsiya).
        /// </summary>
        public AuctionParticipant? WinnerParticipant { get; set; }

        /// <summary>
        /// Xaridorning taklifi Id'si.
        /// </summary>
        public Guid? WinningBidId { get; set; }

        /// <summary>
        /// Sotish narxi, so'm (xaridor taklifining summasi; ikkinchi o'rinda uning o'z taklifi).
        /// </summary>
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// Xaridor yakuniy to'lovni shu vaqtgacha qilishi kerak.
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

        /// <summary>
        /// Sotish takliflari: 1-o'rin (g'olib) va kerak bo'lsa 2-o'rin.
        /// </summary>
        public ICollection<AuctionAward> Awards { get; set; } = [];
    }
}

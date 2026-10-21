using Domain.Abstractions;

namespace Domain.Entities
{
    /// <summary>
    /// Savdodagi taklif. O'zgartirilmaydi va o'chirilmaydi: savdo tarixi shu.
    /// Taklif vaqti — <see cref="AudiTableEntity.CreatedAt"/>, beruvchi — <see cref="ParticipantId"/>.
    /// </summary>
    public class Bid : AudiTableEntity
    {
        /// <summary>
        /// Auksion Id'si (FK).
        /// </summary>
        public Guid AuctionId { get; set; }

        /// <summary>
        /// Auksion (navigatsiya).
        /// </summary>
        public Auction? Auction { get; set; }

        /// <summary>
        /// Taklif bergan ishtirokchi Id'si (FK).
        /// </summary>
        public Guid ParticipantId { get; set; }

        /// <summary>
        /// Ishtirokchi (navigatsiya).
        /// </summary>
        public AuctionParticipant? Participant { get; set; }

        /// <summary>
        /// Taklif summasi, so'm. Auksion ichida takrorlanmaydi: bir xil summadagi ikki taklifdan faqat biri saqlanadi.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Taklif yuborilgan IP manzil.
        /// </summary>
        public string? CreatedByIp { get; set; }
    }
}

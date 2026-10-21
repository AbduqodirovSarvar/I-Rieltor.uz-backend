using Domain.Abstractions;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Obyektni sotib olish taklifi: 1-o'rin (g'olib) va, g'olib voz kechsa yoki to'lamasa, 2-o'rin.
    /// Bir auksionda bir vaqtda faqat bitta faol (taklif qilingan yoki qabul qilingan) taklif bo'ladi.
    /// </summary>
    public class AuctionAward : AudiTableEntity
    {
        /// <summary>
        /// Natija Id'si (FK).
        /// </summary>
        public Guid AuctionResultId { get; set; }

        /// <summary>
        /// Natija (navigatsiya).
        /// </summary>
        public AuctionResult? AuctionResult { get; set; }

        /// <summary>
        /// O'rin: 1 = g'olib, 2 = ikkinchi o'rin.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Taklif qilingan ishtirokchi Id'si (FK).
        /// </summary>
        public Guid ParticipantId { get; set; }

        /// <summary>
        /// Ishtirokchi (navigatsiya).
        /// </summary>
        public AuctionParticipant? Participant { get; set; }

        /// <summary>
        /// Shu ishtirokchining eng yuqori taklifi Id'si (FK); narx shundan.
        /// </summary>
        public Guid BidId { get; set; }

        /// <summary>
        /// Taklif (navigatsiya).
        /// </summary>
        public Bid? Bid { get; set; }

        /// <summary>
        /// Sotish narxi, so'm: ishtirokchining o'z eng yuqori taklifi.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Holat. G'olib uchun ACCEPTED dan boshlanadi, ikkinchi o'rin uchun OFFERED dan.
        /// </summary>
        public AwardStatus Status { get; set; }

        /// <summary>
        /// Ikkinchi o'rin taklifga shu vaqtgacha javob berishi kerak; g'olibda null.
        /// </summary>
        public DateTime? RespondBy { get; set; }

        /// <summary>
        /// Yakuniy to'lov muddati (qabul qilingandan keyin).
        /// </summary>
        public DateTime? PaymentDueAt { get; set; }

        /// <summary>
        /// Javob berilgan yoki holat yakunlangan vaqt.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
    }
}

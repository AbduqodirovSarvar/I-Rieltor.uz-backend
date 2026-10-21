using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Pul harakati: zakalat, yakuniy to'lov yoki zakalatni qaytarish. Holati to'lov tizimining javobi bilan o'zgaradi.
    /// </summary>
    public class Payment : AudiTableEntity
    {
        /// <summary>
        /// To'lovchi (qaytarishda oluvchi) foydalanuvchi Id'si (FK).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Foydalanuvchi (navigatsiya).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Auksion Id'si (FK).
        /// </summary>
        public Guid AuctionId { get; set; }

        /// <summary>
        /// Auksion (navigatsiya).
        /// </summary>
        public Auction? Auction { get; set; }

        /// <summary>
        /// Ishtirokchi Id'si (FK): zakalat va qaytarish shu arizaga tegishli.
        /// </summary>
        public Guid? ParticipantId { get; set; }

        /// <summary>
        /// Ishtirokchi (navigatsiya).
        /// </summary>
        public AuctionParticipant? Participant { get; set; }

        /// <summary>
        /// Maqsad.
        /// </summary>
        public PaymentPurpose Purpose { get; set; }

        /// <summary>
        /// Summa, so'm. Musbat.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Holat.
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        /// <summary>
        /// To'lov tizimi.
        /// </summary>
        public PaymentProvider Provider { get; set; }

        /// <summary>
        /// To'lov tizimidagi tranzaksiya Id'si. Bitta tizim ichida takrorlanmaydi: qayta kelgan xabar
        /// (webhook) ikkinchi to'lov bo'lib yozilmaydi.
        /// </summary>
        [Searchable]
        public string? ProviderTransactionId { get; set; }

        /// <summary>
        /// To'langan (qaytarilgan) vaqt.
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Bekor qilingan yoki muvaffaqiyatsiz bo'lgan vaqt.
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Izoh (masalan admin qo'lda kiritganda).
        /// </summary>
        public string? Note { get; set; }
    }
}

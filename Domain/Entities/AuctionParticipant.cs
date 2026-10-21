using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchining auksionda qatnashish arizasi va ishtirokchi sifatidagi holati.
    /// Bir foydalanuvchi bir auksionga bitta (o'chirilmagan) ariza beradi.
    /// </summary>
    public class AuctionParticipant : AudiTableEntity
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
        /// Ariza bergan foydalanuvchi Id'si (FK). Faqat E-IMZO bilan tasdiqlangan akkaunt (Type != NONE).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Foydalanuvchi (navigatsiya).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Ariza holati.
        /// </summary>
        public ParticipantStatus Status { get; set; } = ParticipantStatus.PENDING;

        /// <summary>
        /// Tasdiqlanganda beriladigan ishtirokchi raqami (savdoda ism o'rniga ko'rsatiladi). Auksion ichida takrorlanmaydi.
        /// </summary>
        public int? ParticipantNumber { get; set; }

        /// <summary>
        /// Zakalat holati.
        /// </summary>
        public DepositStatus DepositStatus { get; set; } = DepositStatus.NOT_PAID;

        /// <summary>
        /// E-IMZO bilan imzolangan ariza (PKCS#7, base64, vaqt tamg'asi bilan). P2-04 da to'ldiriladi.
        /// </summary>
        [NotFilterable]
        public string? SignedApplication { get; set; }

        /// <summary>
        /// Imzochining sertifikatdagi PINFL yoki INN'i; ariza egasiniki bilan mos bo'lishi shart.
        /// </summary>
        public string? SignerIdentifier { get; set; }

        /// <summary>
        /// Imzo vaqti (vaqt tamg'asidan).
        /// </summary>
        public DateTime? SignedAt { get; set; }

        /// <summary>
        /// Admin ko'rib chiqqan vaqt.
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Ko'rib chiqqan admin Id'si.
        /// </summary>
        public Guid? ReviewedBy { get; set; }

        /// <summary>
        /// Rad etilganda sabab (foydalanuvchiga ko'rsatiladi).
        /// </summary>
        [Searchable]
        public string? RejectReason { get; set; }
    }
}

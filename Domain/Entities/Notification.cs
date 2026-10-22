using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchiga yuboriladigan xabar (outbox). Xabar biznes amal bilan bitta tranzaksiyada yoziladi,
    /// fon xizmati esa uni yuboradi va muvaffaqiyatsiz bo'lsa qayta urinadi: amal bajarilgan bo'lsa xabar yo'qolmaydi.
    /// Har kanal uchun alohida yozuv.
    /// </summary>
    public class Notification : AuditableEntity
    {
        /// <summary>
        /// Qabul qiluvchi foydalanuvchi Id'si (FK).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Foydalanuvchi (navigatsiya).
        /// </summary>
        public User? User { get; set; }

        public NotificationChannel Channel { get; set; }

        public NotificationType Type { get; set; }

        public NotificationStatus Status { get; set; } = NotificationStatus.PENDING;

        /// <summary>
        /// Yuborilgan manzil (email yoki telefon): xabar yaratilgandagi qiymat, keyin o'zgartirilsa ham saqlanadi.
        /// </summary>
        [Searchable]
        public string Destination { get; set; } = string.Empty;

        /// <summary>
        /// Email mavzusi; SMS'da null.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Xabar matni (oddiy matn).
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Xabar tegishli auksion (bo'lsa).
        /// </summary>
        public Guid? AuctionId { get; set; }

        /// <summary>
        /// Yuborishga urinishlar soni.
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Keyingi urinish vaqti (UTC). Urinish boshlanganda sal oldinga suriladi: xizmat yiqilib qolsa ham xabar yo'qolmaydi,
        /// ikkinchi nusxa esa uni ikki marta yubormaydi.
        /// </summary>
        public DateTime NextAttemptAt { get; set; }

        public DateTime? SentAt { get; set; }

        /// <summary>
        /// Oxirgi xato matni.
        /// </summary>
        public string? LastError { get; set; }
    }
}

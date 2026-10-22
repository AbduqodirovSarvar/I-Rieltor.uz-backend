using Domain.Abstractions;
using Domain.Behaviours;

namespace Domain.Entities
{
    /// <summary>
    /// Jurnal yozuvi: kim, qachon, qaysi so'rovni yubordi va natijasi nima bo'ldi. Faqat o'zgartiruvchi so'rovlar
    /// (POST, PUT, PATCH, DELETE) va shaxsiy hujjatlarni yuklab olish yoziladi. So'rov tanasi va so'rov satri (query)
    /// yozilmaydi: ularda parol, token va shaxsiy ma'lumotlar bo'lishi mumkin. Yozuv o'zgartirilmaydi;
    /// eskilari saqlash muddati (Retention:AuditLogDays) o'tgach o'chiriladi.
    /// </summary>
    public class AuditLog : AudiTableEntity
    {
        /// <summary>
        /// So'rov yuborilgan vaqt (UTC). CreatedAt yozuv bazaga tushgan vaqt (bir necha soniya keyin).
        /// </summary>
        public DateTime OccurredAt { get; set; }

        /// <summary>
        /// So'rov egasi (kirgan bo'lsa). Kirmagan yoki token yaroqsiz bo'lsa null.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Foydalanuvchi (navigatsiya).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Amal: controller va metod nomi (masalan "AuctionController.Publish"). Aniqlanmasa so'rov yo'li.
        /// </summary>
        [Searchable]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// HTTP metodi.
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// So'rov yo'li, query'siz (masalan "/api/auction/publish").
        /// </summary>
        [Searchable]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Javob kodi. Xatolar (401, 403, 409...) ham yoziladi: hujumlar shu yerda ko'rinadi.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Mijoz IP manzili (ishonchli proxy orqali kelgan haqiqiy manzil).
        /// </summary>
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        /// <summary>
        /// So'rov identifikatori (X-Request-ID): server log'lari bilan bog'laydi.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// So'rov davomiyligi, millisekund.
        /// </summary>
        public int DurationMs { get; set; }
    }
}

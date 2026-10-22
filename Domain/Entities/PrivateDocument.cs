using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchining shaxsiy hujjati (pasport nusxasi, ishonchnoma, to'lov cheki). Ommaviy fayllardan (<see cref="UploadedFile"/>)
    /// farqli, u /uploads orqali berilmaydi: yopiq papkada turadi va faqat egasiga yoki READ_PRIVATEDOCUMENT ruxsatiga
    /// ega xodimga, kirish tekshirilgach beriladi.
    /// </summary>
    public class PrivateDocument : AuditableEntity
    {
        /// <summary>
        /// Hujjat egasi (yuklagan foydalanuvchi) Id'si (FK).
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Egasi (navigatsiya).
        /// </summary>
        public User? Owner { get; set; }

        public DocumentPurpose Purpose { get; set; } = DocumentPurpose.OTHER;

        /// <summary>
        /// Qaysi auksionga tegishli (bo'lsa): xodim arizani ko'rib chiqayotganda ilovalarni shu bo'yicha topadi.
        /// </summary>
        public Guid? AuctionId { get; set; }

        /// <summary>
        /// Qaysi to'lovga tegishli (bo'lsa), masalan bank o'tkazmasi cheki.
        /// </summary>
        public Guid? PaymentId { get; set; }

        /// <summary>
        /// Yuklangan fayl nomi (faqat ko'rsatish va yuklab olish uchun; diskdagi nom emas).
        /// </summary>
        [Searchable]
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// Diskdagi nom (tasodifiy, foydalanuvchi kiritmagan): yo'l bilan hujum mumkin emas.
        /// </summary>
        public string StoredName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }

        [Searchable]
        public string? Description { get; set; }
    }
}

using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Serverga yuklangan fayl (rasm, hujjat, video) haqidagi ma'lumot.
    /// Faylning o'zi diskda (wwwroot/uploads) turadi, bu yerda faqat uning metama'lumotlari saqlanadi.
    /// </summary>
    public class UploadedFile : AuditableEntity
    {
        /// <summary>
        /// Diskdagi fayl nomi (GUID + kengaytma). Noyob.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Faylning wwwroot ichidagi nisbiy yo'li (masalan "/uploads/slide/abc.png").
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Faylni brauzerda ochish uchun to'liq URL manzil. Noyob.
        /// </summary>
        [UriValidation]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Fayl turi (MIME). Fayl kengaytmasiga qarab aniqlanadi.
        /// </summary>
        public FileMimeType Type { get; set; } = FileMimeType.Unknown;

        /// <summary>
        /// Fayl hajmi, baytlarda.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Fayl haqida qisqacha izoh (ixtiyoriy).
        /// </summary>
        public string? Description { get; set; } = null;

        /// <summary>
        /// Rasm ko'rinmaganda chiqadigan muqobil matn (HTML alt atributi, SEO uchun ham kerak).
        /// </summary>
        public string? Alt { get; set; } = null;

        /// <summary>
        /// Fayl qaysi turdagi entity'ga tegishli (masalan "slide", "company").
        /// Diskda papka nomi sifatida ham ishlatiladi.
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Fayl tegishli bo'lgan yozuvning Id'si (ixtiyoriy, bazada FK emas).
        /// </summary>
        public Guid? EntityId { get; set; }
    }
}

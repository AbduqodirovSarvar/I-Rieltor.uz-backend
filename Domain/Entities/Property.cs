using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Auksionga qo'yiladigan ko'chmas mulk obyekti. Narx va savdo shartlari obyektda emas, auksionda
    /// (<see cref="Auction"/>): bir obyekt bir necha marta (masalan bo'lib o'tmagan savdodan keyin) qo'yilishi mumkin.
    /// </summary>
    public class Property : AudiTableEntity
    {
        /// <summary>
        /// Katalogdagi sarlavha (masalan "3 xonali kvartira, Chilonzor").
        /// </summary>
        [Searchable]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Batafsil tavsif.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Toifa Id'si (FK).
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Toifa (navigatsiya).
        /// </summary>
        public PropertyCategory? Category { get; set; }

        /// <summary>
        /// Holat; yangi obyekt qoralama sifatida yaratiladi.
        /// </summary>
        public PropertyStatus Status { get; set; } = PropertyStatus.DRAFT;

        /// <summary>
        /// Viloyat (yoki Toshkent shahri).
        /// </summary>
        [Searchable]
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Tuman yoki shahar.
        /// </summary>
        [Searchable]
        public string? District { get; set; }

        /// <summary>
        /// To'liq manzil.
        /// </summary>
        [Searchable]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Xaritadagi kenglik.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Xaritadagi uzunlik.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Kadastr raqami. Berilgan bo'lsa, o'chirilmagan obyektlar orasida takrorlanmaydi.
        /// </summary>
        [Searchable]
        public string? CadastralNumber { get; set; }

        /// <summary>
        /// Umumiy maydon, kv. metr.
        /// </summary>
        public decimal? AreaSqm { get; set; }

        /// <summary>
        /// Yer maydoni, sotix (uy, yer uchastkasi uchun).
        /// </summary>
        public decimal? LandAreaSotix { get; set; }

        /// <summary>
        /// Xonalar soni.
        /// </summary>
        public int? Rooms { get; set; }

        /// <summary>
        /// Qavat.
        /// </summary>
        public int? Floor { get; set; }

        /// <summary>
        /// Binoning qavatlar soni.
        /// </summary>
        public int? TotalFloors { get; set; }

        /// <summary>
        /// Qurilgan yili.
        /// </summary>
        public int? BuildYear { get; set; }

        /// <summary>
        /// Baholangan qiymati, so'm (ma'lumot uchun; savdo narxi auksionda).
        /// </summary>
        public decimal? EstimatedValue { get; set; }

        /// <summary>
        /// Sotuvchi (mulk egasi yoki savdoga qo'ygan tashkilot) nomi.
        /// </summary>
        [Searchable]
        public string? SellerName { get; set; }

        /// <summary>
        /// Katalogda ko'rilganlar soni.
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// Rasmlar, hujjatlar, videolar.
        /// </summary>
        public ICollection<PropertyMedia> Media { get; set; } = [];
    }
}

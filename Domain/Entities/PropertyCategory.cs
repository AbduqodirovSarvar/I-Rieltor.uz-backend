using Domain.Abstractions;
using Domain.Behaviours;

namespace Domain.Entities
{
    /// <summary>
    /// Ko'chmas mulk toifasi (masalan "Turar joy" → "Kvartira"). Ichma-ich bo'lishi mumkin.
    /// </summary>
    public class PropertyCategory : AuditableEntity
    {
        /// <summary>
        /// Toifa nomi. O'chirilmagan toifalar orasida takrorlanmaydi (katta-kichik harfga qaramay).
        /// </summary>
        [Searchable]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Qisqa tavsif.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Yuqori toifa Id'si (FK). Eng yuqori darajadagi toifada null.
        /// </summary>
        public Guid? ParentCategoryId { get; set; }

        /// <summary>
        /// Yuqori toifa (navigatsiya).
        /// </summary>
        public PropertyCategory? ParentCategory { get; set; }

        /// <summary>
        /// Ro'yxatdagi tartib raqami (kichigi oldin).
        /// </summary>
        public int SortOrder { get; set; }
    }
}

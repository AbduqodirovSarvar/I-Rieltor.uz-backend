using Domain.Abstractions;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Obyektga biriktirilgan fayl: rasm, hujjat yoki video. Fayl o'zi <see cref="UploadedFile"/>da.
    /// </summary>
    public class PropertyMedia : AuditableEntity
    {
        /// <summary>
        /// Obyekt Id'si (FK).
        /// </summary>
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Obyekt (navigatsiya).
        /// </summary>
        public Property? Property { get; set; }

        /// <summary>
        /// Fayl Id'si (FK). Bitta fayl obyektga bir marta biriktiriladi.
        /// </summary>
        public Guid UploadedFileId { get; set; }

        /// <summary>
        /// Fayl (navigatsiya).
        /// </summary>
        public UploadedFile? UploadedFile { get; set; }

        /// <summary>
        /// Fayl turi.
        /// </summary>
        public PropertyMediaKind Kind { get; set; } = PropertyMediaKind.IMAGE;

        /// <summary>
        /// Hujjat yoki rasm nomi (masalan "Kadastr pasporti").
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Galereyadagi tartib raqami (kichigi oldin).
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Katalogdagi asosiy rasm. Obyektda ko'pi bilan bittasi.
        /// </summary>
        public bool IsCover { get; set; }
    }
}

using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Ko'chmas mulk obyektiga biriktirilgan hujjat (kadastr, guvohnoma, shartnoma va h.k.).
    /// </summary>
    public class PropertyFiles : AudiTableEntity
    {
        /// <summary>
        /// Hujjat nomi (masalan "Kadastr pasporti").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Hujjat haqida izoh.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Hujjat tegishli bo'lgan obyektning Id'si (FK).
        /// </summary>
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Hujjat tegishli bo'lgan obyekt (navigatsiya).
        /// </summary>
        public Property? Property { get; set; } = null;

        /// <summary>
        /// Yuklangan hujjat faylining Id'si (FK).
        /// </summary>
        public Guid UploadedFileId { get; set; }

        /// <summary>
        /// Yuklangan hujjat fayli (navigatsiya).
        /// </summary>
        public UploadedFile? UploadedFile { get; set; } = null;
    }
}

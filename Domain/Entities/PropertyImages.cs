using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Ko'chmas mulk obyektining rasmi (galereya uchun). Obyekt bilan faylni bog'laydi.
    /// </summary>
    public class PropertyImages : AudiTableEntity
    {
        /// <summary>
        /// Rasm tegishli bo'lgan obyektning Id'si (FK).
        /// </summary>
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Rasm tegishli bo'lgan obyekt (navigatsiya).
        /// </summary>
        public Property? Property { get; set; } = null;

        /// <summary>
        /// Rasm faylining Id'si (FK).
        /// </summary>
        public Guid UploadedFileId { get; set; }

        /// <summary>
        /// Rasm fayli (navigatsiya).
        /// </summary>
        public UploadedFile? UploadedFile { get; set; } = null;
    }
}

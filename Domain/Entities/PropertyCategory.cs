using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Ko'chmas mulk kategoriyasi. Ichma-ich (daraxt) bo'lishi mumkin,
    /// masalan "Turar joy" ichida "Kvartira" va "Hovli".
    /// </summary>
    public class PropertyCategory : AudiTableEntity
    {
        /// <summary>
        /// Kategoriya nomi.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kategoriya haqida izoh.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Ota kategoriyaning Id'si. Eng yuqori darajadagi kategoriyada null bo'ladi.
        /// </summary>
        public Guid? ParentCategoryId { get; set; } = null;

        /// <summary>
        /// Ota kategoriya (navigatsiya).
        /// </summary>
        public PropertyCategory? ParentCategory { get; set; } = null;
    }
}

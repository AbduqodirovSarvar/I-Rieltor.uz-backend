using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Sotuvga yoki auksionga qo'yiladigan ko'chmas mulk obyekti (uy, kvartira, yer, bino va h.k.).
    /// </summary>
    public class Property : AudiTableEntity
    {
        /// <summary>
        /// Obyekt nomi yoki e'lon sarlavhasi.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Obyektning batafsil tavsifi.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Obyekt kategoriyasining Id'si (FK).
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Obyekt kategoriyasi (navigatsiya).
        /// </summary>
        public PropertyCategory? Category { get; set; } = null;

        /// <summary>
        /// Boshlang'ich narx: auksion shu summadan boshlanadi.
        /// </summary>
        public decimal InitialPrice { get; set; }

        /// <summary>
        /// Obyekt sahifasi necha marta ko'rilgani (statistika uchun).
        /// </summary>
        public double ViewCount { get; set; }

        /// <summary>
        /// Obyektning sotuvdagi holati, matn ko'rinishida (<see cref="Enums.Status"/> qiymatlari).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Obyekt egasi (ism yoki tashkilot nomi, oddiy matn).
        /// </summary>
        public string Owner { get; set; } = string.Empty;
    }
}

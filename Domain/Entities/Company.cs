using Domain.Abstractions;
using Domain.Behaviours;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Sayt egasi bo'lgan kompaniya haqidagi ma'lumot ("Biz haqimizda" va "Aloqa" sahifalari uchun).
    /// </summary>
    public class Company : AudiTableEntity
    {
        /// <summary>
        /// Kompaniya nomi.
        /// </summary>
        [Searchable]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kompaniya haqida umumiy ma'lumot.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Kompaniya missiyasi (ixtiyoriy).
        /// </summary>
        public string? Mission { get; set; }

        /// <summary>
        /// Ofis manzili, matn ko'rinishida.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Ofis joylashuviga xaritadagi havola (masalan Google yoki Yandex xarita URL'i).
        /// </summary>
        [Url]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Asosiy aloqa telefoni.
        /// </summary>
        [PhoneNumberValidation]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Qo'shimcha aloqa telefoni (ixtiyoriy).
        /// </summary>
        [PhoneNumberValidation]
        public string? PhoneNumber2 { get; set; }

        /// <summary>
        /// Kompaniyaning aloqa uchun elektron pochtasi (ixtiyoriy).
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Kompaniya logotipi faylining Id'si (FK, ixtiyoriy).
        /// </summary>
        public Guid? LogoFileId { get; set; }

        /// <summary>
        /// Kompaniya logotipi fayli (navigatsiya).
        /// </summary>
        public UploadedFile? LogoFile { get; set; } = null;

        /// <summary>
        /// Ofis joylashuvining geografik kengligi (latitude).
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Ofis joylashuvining geografik uzunligi (longitude).
        /// </summary>
        public decimal? Longitude { get; set; }

    }
}

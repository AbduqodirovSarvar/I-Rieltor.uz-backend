using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchining auksionda qatnashish uchun bergan arizasi.
    /// Ariza tasdiqlangandan keyingina foydalanuvchi savdoda qatnasha oladi.
    /// </summary>
    public class AuctionApplications : AudiTableEntity
    {
        /// <summary>
        /// Ariza berilgan auksionning Id'si (FK).
        /// </summary>
        public Guid AuctionId { get; set; }

        /// <summary>
        /// Ariza berilgan auksion (navigatsiya).
        /// </summary>
        public Auction? Auction { get; set; } = null;

        /// <summary>
        /// Ariza beruvchi foydalanuvchining Id'si (FK).
        /// </summary>
        public Guid ApplicantId { get; set; }

        /// <summary>
        /// Ariza beruvchi foydalanuvchi (navigatsiya).
        /// </summary>
        public User? Applicant { get; set; } = null;

        /// <summary>
        /// Arizaga ilova qilingan fayl (imzolangan ariza, to'lov cheki va h.k.) Id'si (FK).
        /// </summary>
        public Guid ApplicationFileId { get; set; }

        /// <summary>
        /// Arizaga ilova qilingan fayl (navigatsiya).
        /// </summary>
        public UploadedFile? ApplicationFile { get; set; } = null;

        /// <summary>
        /// Ishtirokchi taklif qilgan narx.
        /// </summary>
        public decimal OfferedAmount { get; set; }

        /// <summary>
        /// Arizaning ko'rib chiqilish holati, matn ko'rinishida (masalan kutilmoqda, rad etildi).
        /// </summary>
        public string ApplicationStatus { get; set; } = string.Empty;

        /// <summary>
        /// Ariza tasdiqlanganmi: true bo'lsa foydalanuvchi savdoda qatnasha oladi.
        /// </summary>
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// Ariza tasdiqlangan vaqt. Tasdiqlanmagan bo'lsa null.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }
    }
}

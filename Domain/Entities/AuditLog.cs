using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchi amallari jurnali: kim, qachon, qaysi yozuv ustida qanday amal bajargani.
    /// Xavfsizlik nazorati va nizolarni tekshirish uchun kerak.
    /// </summary>
    public class AuditLog : AudiTableEntity
    {
        /// <summary>
        /// Bajarilgan amal nomi (masalan "UPDATE_USER", "DELETE_SLIDE").
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Amal bajarilgan yozuvning Id'si.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Amal bajarilgan entity turi nomi (masalan "User", "Slide").
        /// </summary>
        public string EntityTypeName { get; set; } = string.Empty;

        /// <summary>
        /// So'rov yuborilgan IP manzil.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// So'rov yuborgan brauzer yoki ilova haqidagi ma'lumot (User-Agent sarlavhasi).
        /// </summary>
        public string UserAgent {  get; set; } = string.Empty;

        /// <summary>
        /// Amalni bajargan foydalanuvchining Id'si, matn ko'rinishida.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Amalni bajargan foydalanuvchi (navigatsiya).
        /// </summary>
        public User? User { get; set; }
    }
}

using Domain.Behaviours;
using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Rolga berilgan bitta ruxsat. Bitta rol uchun har bir ruxsat faqat bir marta yoziladi.
    /// </summary>
    public class UserRolePermission : AudiTableEntity
    {
        /// <summary>
        /// Ruxsat tegishli bo'lgan rolning Id'si (FK).
        /// </summary>
        public Guid UserRoleId { get; set; }

        /// <summary>
        /// Ruxsat tegishli bo'lgan rol (navigatsiya).
        /// </summary>
        public UserRole? UserRole { get; set; }

        /// <summary>
        /// Ruxsat nomi: <c>Domain.Enums.Actions</c> enum'laridagi qiymatlardan biri
        /// (masalan "CREATE_USER", "READ_SLIDE").
        /// </summary>
        [Searchable]
        public string Permission { get; set; } = "NONE";
    }
}

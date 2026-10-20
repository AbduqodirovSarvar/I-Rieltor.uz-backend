using Domain.Behaviours;
using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchi roli (masalan SUPER_ADMIN, DEFAULT_USER). Ruxsatlarni bir guruhga jamlaydi.
    /// </summary>
    public class UserRole : AudiTableEntity
    {
        /// <summary>
        /// Rolning noyob nomi (masalan "SUPER_ADMIN").
        /// </summary>
        [Searchable]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rol nimaga mo'ljallangani haqida izoh (ixtiyoriy).
        /// </summary>
        [Searchable]
        public string? Description { get; set; } = null;

        /// <summary>
        /// Shu rolga biriktirilgan foydalanuvchilar.
        /// </summary>
        public ICollection<User> Users = [];

        /// <summary>
        /// Shu rolga berilgan ruxsatlar.
        /// </summary>
        public ICollection<UserRolePermission> Permissions = [];
    }
}

using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UserRole : AudiTableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        public ICollection<UserRolePermission> Permissions = [];
    }
}

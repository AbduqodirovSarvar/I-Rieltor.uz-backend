using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UserRolePermission : AudiTableEntity
    {
        public Guid UserRoleId { get; set; }
        public UserRole? UserRole { get; set; }
        public string Permission { get; set; } = "NONE";
    }
}

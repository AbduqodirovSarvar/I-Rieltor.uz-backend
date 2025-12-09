using Application.Models.Dtos.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.UserRolePermissions
{
    public record UpdateUserRolePermissionDto : BaseUpdateDto
    {
        public UserRole? UserRole { get; set; }
        public string Permission { get; set; } = "NONE";
    }
}

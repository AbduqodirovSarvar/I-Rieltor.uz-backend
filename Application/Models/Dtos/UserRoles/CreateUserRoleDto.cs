using Application.Models.Dtos.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.UserRoles
{
    public record CreateUserRoleDto : BaseCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
    }
}

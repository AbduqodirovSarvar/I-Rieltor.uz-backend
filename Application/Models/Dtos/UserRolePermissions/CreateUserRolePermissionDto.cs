using Application.Models.Dtos.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.UserRolePermissions
{
    public record CreateUserRolePermissionDto : BaseCreateDto
    {
        [Required]
        public Guid UserRoleId { get; set; }
        [Required]
        public string Permission { get; set; } = "NONE";
    }
}

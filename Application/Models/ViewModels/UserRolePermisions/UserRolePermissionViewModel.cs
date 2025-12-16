using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.UserRolePermisions
{
    public record UserRolePermissionViewModel : BaseViewModel
    {
        public Guid UserRoleId { get; set; }
        public UserRoleViewModel? UserRole { get; set; }
        public string Permission { get; set; } = "NONE";
    }
}

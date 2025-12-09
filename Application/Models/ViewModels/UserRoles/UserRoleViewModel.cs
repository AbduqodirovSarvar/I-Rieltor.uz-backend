using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.UserRoles
{
    public record UserRoleViewModel : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        public ICollection<UserRolePermissionViewModel> Permissions { get; set; } = [];
    }
}

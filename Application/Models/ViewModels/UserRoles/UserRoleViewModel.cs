using Application.Models.ViewModels.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.UserRole
{
    public record UserRoleViewModel : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        public ICollection<UserRolePermissionViewModel> Permissions { get; set; } = [];
    }
}

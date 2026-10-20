using Application.Models.ViewModels.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.UserRolePermisions
{
    /// <summary>
    /// The role is referenced by Id only: a nested role would point back to its permissions
    /// and make the JSON endless (role → permissions → role → ...).
    /// </summary>
    public record UserRolePermissionViewModel : BaseViewModel
    {
        public Guid UserRoleId { get; set; }
        public string Permission { get; set; } = "NONE";
    }
}

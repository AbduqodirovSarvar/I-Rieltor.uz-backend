using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.UserRoles;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Application.Features.UserRoles.Commands.Update
{
    public record UpdateUserRoleCommand
        : UpdateUserRoleDto,
        ICommand<ResponseModel<UserRoleViewModel>>
    {
    }
}

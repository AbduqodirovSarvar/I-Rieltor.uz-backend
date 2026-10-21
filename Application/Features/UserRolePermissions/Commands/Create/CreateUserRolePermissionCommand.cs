using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.UserRolePermissions;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Commands.Create
{
    public record CreateUserRolePermissionCommand
        : CreateUserRolePermissionDto, 
        ICommand<ResponseModel<UserRolePermissionViewModel>>
    {
    }
}

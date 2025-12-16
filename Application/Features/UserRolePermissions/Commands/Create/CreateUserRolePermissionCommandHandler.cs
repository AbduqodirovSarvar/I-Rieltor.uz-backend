using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Commands.Create
{
    public class CreateUserRolePermissionCommandHandler(
        IUserRolePermissionService userRolePermissionService)
        : ICommandHandler<CreateUserRolePermissionCommand, ResponseModel<UserRolePermissionViewModel>>
    {
        private readonly IUserRolePermissionService _userRolePermissionService = userRolePermissionService;
        public async Task<ResponseModel<UserRolePermissionViewModel>> Handle(CreateUserRolePermissionCommand request, CancellationToken cancellationToken)
        {
            return await _userRolePermissionService.CreateAsync(request, cancellationToken);
        }
    }
}

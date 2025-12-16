using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Commands.Delete
{
    public class DeleteUserRolePermissionCommandHandler(
        IUserRolePermissionService userRolePermissionService)
        : ICommandHandler<DeleteUserRolePermissionCommand, ResponseModel<bool>>
    {
        private readonly IUserRolePermissionService _userRolePermissionService = userRolePermissionService;
        public async Task<ResponseModel<bool>> Handle(DeleteUserRolePermissionCommand request, CancellationToken cancellationToken)
        {
            return await _userRolePermissionService.DeleteAsync(request.Id, cancellationToken);
        }
    }
}

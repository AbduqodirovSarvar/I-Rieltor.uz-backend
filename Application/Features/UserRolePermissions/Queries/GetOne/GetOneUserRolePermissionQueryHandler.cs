using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Queries.GetOne
{
    public class GetOneUserRolePermissionQueryHandler(
        IUserRolePermissionService userRolePermissionService)
        : IQueryHandler<GetOneUserRolePermissionQuery, ResponseModel<UserRolePermissionViewModel?>>
    {
        private readonly IUserRolePermissionService _userRolePermissionService = userRolePermissionService;
        public async Task<ResponseModel<UserRolePermissionViewModel?>> Handle(GetOneUserRolePermissionQuery request, CancellationToken cancellationToken)
        {
            return await _userRolePermissionService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}

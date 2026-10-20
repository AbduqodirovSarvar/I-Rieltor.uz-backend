using Application.Extentions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using Application.Services.EntityServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Queries.GetList
{
    public class GetUserRolePermissionListQueryHandler(
        IUserRolePermissionService userRolePermissionService)
        : IQueryHandler<GetUserRolePermissionListQuery, ResponseModel<List<UserRolePermissionViewModel>>>
    {
        private readonly IUserRolePermissionService _userRolePermissionService = userRolePermissionService;
        public async Task<ResponseModel<List<UserRolePermissionViewModel>>> Handle(GetUserRolePermissionListQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<UserRolePermission, bool>>? predicate = null;
            if (request.Filters != null && request.Filters.Any())
            {
                predicate = request.Filters.BuildPredicate<UserRolePermission>();
            }
            return await _userRolePermissionService.GetAllAsync(predicate, request.PageIndex, request.PageSize, cancellationToken);
        }
    }
}

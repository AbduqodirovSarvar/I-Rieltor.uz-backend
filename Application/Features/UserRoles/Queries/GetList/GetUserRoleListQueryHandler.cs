using Application.Extensions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using Application.Services.EntityServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Queries.GetList
{
    public class GetUserRoleListQueryHandler(
        IUserRoleService userRoleService)
        : IQueryHandler<GetUserRoleListQuery, ResponseModel<List<UserRoleViewModel>>>
    {
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<ResponseModel<List<UserRoleViewModel>>> Handle(GetUserRoleListQuery request, CancellationToken cancellationToken)
        {
            var predicate = request.BuildPredicate<UserRole>();
            var orderBy = request.BuildOrderBy<UserRole>();
            return await _userRoleService.GetAllAsync(predicate, request.PageIndex, request.PageSize, orderBy, cancellationToken);
        }
    }
}

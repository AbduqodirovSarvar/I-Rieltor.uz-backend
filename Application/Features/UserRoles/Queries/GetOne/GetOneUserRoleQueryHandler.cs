using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Queries.GetOne
{
    public class GetOneUserRoleQueryHandler(
        IUserRoleService userRoleService)
        : IQueryHandler<GetOneUserRoleQuery, ResponseModel<UserRoleViewModel?>>
    {
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<ResponseModel<UserRoleViewModel?>> Handle(GetOneUserRoleQuery request, CancellationToken cancellationToken)
        {
            return await _userRoleService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}

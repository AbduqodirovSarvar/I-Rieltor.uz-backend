using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Queries.GetList
{
    public record GetUserRolePermissionListQuery
        : BaseGetListQuery,
        IQuery<ResponseModel<List<UserRolePermissionViewModel>>>
    {
    }
}

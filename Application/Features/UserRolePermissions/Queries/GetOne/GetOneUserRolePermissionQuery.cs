using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRolePermissions.Queries.GetOne
{
    public record GetOneUserRolePermissionQuery
        : BaseGetOneQuery,
        IQuery<ResponseModel<UserRolePermissionViewModel?>>
    {
    }
}

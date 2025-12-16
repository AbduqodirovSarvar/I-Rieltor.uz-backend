using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Queries.GetList
{
    public record GetUserRoleListQuery
        : BaseGetListQuery, IQuery<ResponseModel<List<UserRoleViewModel>>>
    {
    }
}

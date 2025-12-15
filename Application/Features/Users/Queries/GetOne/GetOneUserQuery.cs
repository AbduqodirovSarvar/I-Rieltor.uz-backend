using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Queries.GetOne
{
    public record GetOneUserQuery
        : BaseGetOneQuery,
        IQuery<ResponseModel<UserViewModel?>>
    {
    }
}

using Application.Extensions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Queries.GetList
{
    public class GetUserListQueryHandler(
        IUserService userService)
        : IQueryHandler<GetUserListQuery, ResponseModel<List<UserViewModel>>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<List<UserViewModel>>> Handle(GetUserListQuery request, CancellationToken cancellationToken)
        {
            var predicate = request.BuildPredicate<User>();
            var orderBy = request.BuildOrderBy<User>();
            return await _userService.GetAllAsync(predicate, request.PageIndex, request.PageSize, orderBy, cancellationToken);
        }
    }
}

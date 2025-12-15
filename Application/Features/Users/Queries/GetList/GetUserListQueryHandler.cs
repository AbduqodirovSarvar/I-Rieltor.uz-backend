using Application.Extentions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
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
            Expression<Func<Domain.Entities.User, bool>>? predicate = null;
            if (request.Filters != null && request.Filters.Any())
            {
                predicate = request.Filters.BuildPredicate<Domain.Entities.User>();
            }
            return await _userService.GetAllAsync(predicate, request.PageIndex, request.PageSize, cancellationToken);
        }
    }
}

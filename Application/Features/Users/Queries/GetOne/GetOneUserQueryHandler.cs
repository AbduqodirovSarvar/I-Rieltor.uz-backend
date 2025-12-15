using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Queries.GetOne
{
    public class GetOneUserQueryHandler(
        IUserService userService)
        : IQueryHandler<GetOneUserQuery, ResponseModel<UserViewModel?>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<UserViewModel?>> Handle(GetOneUserQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}

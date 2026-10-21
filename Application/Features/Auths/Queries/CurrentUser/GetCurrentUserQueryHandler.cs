using Application.Exceptions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using AutoMapper;

namespace Application.Features.Auths.Queries.CurrentUser
{
    public class GetCurrentUserQueryHandler(IUserService userService, IMapper mapper)
        : IQueryHandler<GetCurrentUserQuery, ResponseModel<UserViewModel>>
    {
        private readonly IUserService _userService = userService;
        private readonly IMapper _mapper = mapper;
        public async Task<ResponseModel<UserViewModel>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            // A valid token whose user was deleted counts as signed out.
            var user = await _userService.GetCurrentUser(cancellationToken)
                            ?? throw new UnauthenticatedException("User not found.");

            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(user));
        }
    }
}

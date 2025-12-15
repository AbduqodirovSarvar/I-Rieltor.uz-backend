using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Commands.Create
{
    public class CreateUserCommandHandler(
        IUserService userService)
        : ICommandHandler<CreateUserCommand, ResponseModel<UserViewModel>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<UserViewModel>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userExists = await _userService.ExistsAsync(x => x.Login == request.Login
                                                           || x.Email == request.Email
                                                           || x.INNPINFL == request.INNPINFL,
                                                           cancellationToken);
            if (userExists.Data)
            {
                throw new InvalidOperationException("User with this login, email, or INNPINFL already exists.");
            }
            return await _userService.CreateAsync(request, cancellationToken);
        }
    }
}

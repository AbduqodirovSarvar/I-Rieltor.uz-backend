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
            // Uniqueness of login, email, INNPINFL and phone is checked inside the service.
            return await _userService.CreateAsync(request, cancellationToken);
        }
    }
}

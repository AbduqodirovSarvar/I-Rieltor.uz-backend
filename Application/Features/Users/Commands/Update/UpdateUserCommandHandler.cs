using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Commands.Update
{
    public class UpdateUserCommandHandler(
        IUserService userService)
        : ICommandHandler<UpdateUserCommand, ResponseModel<UserViewModel>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<UserViewModel>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.UpdateAsync(request, cancellationToken);
        }
    }
}

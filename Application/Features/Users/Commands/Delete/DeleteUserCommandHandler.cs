using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Commands.Delete
{
    public class DeleteUserCommandHandler(
        IUserService userService)
        : ICommandHandler<DeleteUserCommand, ResponseModel<bool>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.DeleteAsync(request.Id, cancellationToken); 
        }
    }
}

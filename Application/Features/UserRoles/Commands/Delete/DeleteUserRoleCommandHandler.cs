using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Commands.Delete
{
    public class DeleteUserRoleCommandHandler(
        IUserRoleService userRoleService)
        : ICommandHandler<DeleteUserRoleCommand, ResponseModel<bool>>
    {
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<ResponseModel<bool>> Handle(DeleteUserRoleCommand request, CancellationToken cancellationToken)
        {
            return await _userRoleService.DeleteAsync(request.Id, cancellationToken);
        }
    }
}

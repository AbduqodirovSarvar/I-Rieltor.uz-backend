using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Commands.Update
{
    public class UpdateUserRoleCommandHandler(
        IUserRoleService userRoleService)
        : ICommandHandler<UpdateUserRoleCommand, ResponseModel<UserRoleViewModel>>
    {
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<ResponseModel<UserRoleViewModel>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            return await _userRoleService.UpdateAsync(request, cancellationToken);
        }
    }
}

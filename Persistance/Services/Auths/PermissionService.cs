using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Auths
{
    public class PermissionService(
        ICurrentUserService currentUserService,
        IUserRoleService userRoleService,
        IUserRolePermissionService userRolePermissionService) : IPermissionService
    {
        private readonly IUserRolePermissionService _userRolePermissionService = userRolePermissionService;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<bool> Check(string permission)
        {
            var user = await _currentUserService.GetCurrentUserAsync()
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            if(user.UserRoleId == null)
                throw new UnauthorizedAccessException("You don't have access for this action!");

            var role = await _userRoleService.GetByIdAsync(user.UserRoleId.Value)
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            var result = await _userRolePermissionService.ExistsAsync(x => x.UserRoleId == user.UserRoleId && x.Permission == permission);
            return result.Data;
        }

        public async Task<bool> Check(string permission, User user)
        {
            user ??= await _currentUserService.GetCurrentUserAsync()
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            if (user.UserRoleId == null)
                throw new UnauthorizedAccessException("You don't have access for this action!");

            var role = await _userRoleService.GetByIdAsync(user.UserRoleId.Value)
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            var result = await _userRolePermissionService.ExistsAsync(x => x.UserRoleId == user.UserRoleId && x.Permission == permission);
            return result.Data;
        }
    }
}

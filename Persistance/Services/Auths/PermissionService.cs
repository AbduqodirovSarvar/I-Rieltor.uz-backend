using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
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
        IUserRoleRepository userRoleRepository,
        IUserRolePermissionRepository userRolePermissionRepository) : IPermissionService
    {
        private readonly IUserRolePermissionRepository _userRolePermissionRepository = userRolePermissionRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;
        public async Task<bool> Check(string permission)
        {
            var user = await _currentUserService.GetCurrentUserAsync()
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            if(user.UserRoleId == null)
                throw new UnauthorizedAccessException("You don't have access for this action!");

            var role = await _userRoleRepository.GetByIdAsync(user.UserRoleId.Value)
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            var result = await _userRolePermissionRepository.AnyAsync(x => x.UserRoleId == user.UserRoleId && x.Permission == permission);
            return result;
        }

        public async Task<bool> Check(string permission, User user)
        {
            user ??= await _currentUserService.GetCurrentUserAsync()
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            if (user.UserRoleId == null)
                throw new UnauthorizedAccessException("You don't have access for this action!");

            var role = await _userRoleRepository.GetByIdAsync(user.UserRoleId.Value)
                        ?? throw new UnauthorizedAccessException("You don't have access for this action!");

            var result = await _userRolePermissionRepository.AnyAsync(x => x.UserRoleId == user.UserRoleId && x.Permission == permission);
            return result;
        }

        public Task<IReadOnlyCollection<string>> GetAllPermission()
        {
            var permissionNamespace = "Domain.Enums.Actions";

            var domainAssembly = typeof(Domain.Enums.Actions.BaseActions).Assembly;

            var enumTypes = domainAssembly
                .GetTypes()
                .Where(t => t.IsEnum && t.Namespace == permissionNamespace)
                .ToList();

            var permissions = new List<string>();

            foreach (var enumType in enumTypes)
            {
                var values = Enum.GetNames(enumType);
                permissions.AddRange(values);
            }

            return Task.FromResult((IReadOnlyCollection<string>)permissions.AsReadOnly());
        }

    }
}

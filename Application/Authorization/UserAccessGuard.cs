using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;

namespace Application.Authorization
{
    /// <summary>
    /// "Nobody manages someone more powerful than themselves." The CRUD permission (UPDATE_USER, ...) says what kind of
    /// change a caller may make; this guard says on whom. Every refusal is a <see cref="Application.Exceptions.ForbiddenException"/> (403).
    /// SUPER_ADMIN passes every check; its own role and the last SUPER_ADMIN user are protected even from SUPER_ADMIN.
    /// </summary>
    public interface IUserAccessGuard
    {
        /// <summary>Update another user (or yourself) through the admin endpoints.</summary>
        Task EnsureCanManageUserAsync(User target, CancellationToken cancellationToken = default);

        /// <summary>Delete a user: as <see cref="EnsureCanManageUserAsync"/>, plus not yourself and not the last SUPER_ADMIN.</summary>
        Task EnsureCanDeleteUserAsync(User target, CancellationToken cancellationToken = default);

        /// <summary>Give <paramref name="target"/> the role <paramref name="roleId"/>.</summary>
        Task EnsureCanAssignRoleAsync(User target, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>Add a permission to, or remove it from, a role.</summary>
        Task EnsureCanChangeRolePermissionAsync(Guid roleId, string permission, CancellationToken cancellationToken = default);

        /// <summary>A PINFL/INN routes E-IMZO sign-in to an account, so only SUPER_ADMIN may set one by hand.</summary>
        Task EnsureCanSetIdentifierAsync(CancellationToken cancellationToken = default);
    }

    public sealed class UserAccessGuard(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IUserRolePermissionRepository userRolePermissionRepository) : IUserAccessGuard
    {
        private const string Refused = "You cannot manage a user or role with more rights than your own.";

        private Caller? _caller;

        public async Task EnsureCanManageUserAsync(User target, CancellationToken cancellationToken = default)
        {
            var caller = await GetCallerAsync(cancellationToken);
            if (caller.IsSuperAdmin)
                return;

            if (await IsSuperAdminRoleAsync(target.UserRoleId, cancellationToken)
                || !(await GetRolePermissionsAsync(target.UserRoleId, cancellationToken)).IsSubsetOf(caller.Permissions))
                throw new ForbiddenException(Refused);
        }

        public async Task EnsureCanDeleteUserAsync(User target, CancellationToken cancellationToken = default)
        {
            var caller = await GetCallerAsync(cancellationToken);
            if (target.Id == caller.UserId)
                throw new ForbiddenException("You cannot delete your own account here.");

            await EnsureCanManageUserAsync(target, cancellationToken);

            if (await IsSuperAdminRoleAsync(target.UserRoleId, cancellationToken)
                && !await userRepository.AnyAsync(u => u.Id != target.Id && u.UserRoleId == target.UserRoleId, cancellationToken))
                throw new ForbiddenException("The last SUPER_ADMIN cannot be deleted.");
        }

        public async Task EnsureCanAssignRoleAsync(User target, Guid roleId, CancellationToken cancellationToken = default)
        {
            var caller = await GetCallerAsync(cancellationToken);
            if (target.Id == caller.UserId)
                throw new ForbiddenException("You cannot change your own role.");

            await EnsureCanManageUserAsync(target, cancellationToken);

            // Taking SUPER_ADMIN away from its last holder would leave nobody able to manage the system.
            if (await IsSuperAdminRoleAsync(target.UserRoleId, cancellationToken) && target.UserRoleId != roleId
                && !await userRepository.AnyAsync(u => u.Id != target.Id && u.UserRoleId == target.UserRoleId, cancellationToken))
                throw new ForbiddenException("The last SUPER_ADMIN cannot lose the role.");

            if (caller.IsSuperAdmin)
                return;

            if (await IsSuperAdminRoleAsync(roleId, cancellationToken)
                || !(await GetRolePermissionsAsync(roleId, cancellationToken)).IsSubsetOf(caller.Permissions))
                throw new ForbiddenException(Refused);
        }

        public async Task EnsureCanChangeRolePermissionAsync(Guid roleId, string permission, CancellationToken cancellationToken = default)
        {
            // SUPER_ADMIN holds every permission by definition; the seeder would put a removed one back anyway.
            if (await IsSuperAdminRoleAsync(roleId, cancellationToken))
                throw new ForbiddenException("The permissions of SUPER_ADMIN cannot be changed.");

            var caller = await GetCallerAsync(cancellationToken);
            if (caller.IsSuperAdmin)
                return;

            if (caller.RoleId == roleId)
                throw new ForbiddenException("You cannot change the permissions of your own role.");

            if (!caller.Permissions.Contains(permission)
                || !(await GetRolePermissionsAsync(roleId, cancellationToken)).IsSubsetOf(caller.Permissions))
                throw new ForbiddenException(Refused);
        }

        public async Task EnsureCanSetIdentifierAsync(CancellationToken cancellationToken = default)
        {
            if (!(await GetCallerAsync(cancellationToken)).IsSuperAdmin)
                throw new ForbiddenException("Only SUPER_ADMIN can set a PINFL/INN; it normally comes from the E-IMZO certificate.");
        }

        private sealed record Caller(Guid UserId, Guid? RoleId, bool IsSuperAdmin, HashSet<string> Permissions);

        private async Task<Caller> GetCallerAsync(CancellationToken cancellationToken)
        {
            if (_caller != null)
                return _caller;

            var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException("Sign in first.");
            var user = await userRepository.GetAsync(u => u.Id == userId, cancellationToken)
                       ?? throw new UnauthorizedAccessException("Sign in first.");

            return _caller = new Caller(
                user.Id,
                user.UserRoleId,
                await IsSuperAdminRoleAsync(user.UserRoleId, cancellationToken),
                await GetRolePermissionsAsync(user.UserRoleId, cancellationToken));
        }

        private async Task<bool> IsSuperAdminRoleAsync(Guid? roleId, CancellationToken cancellationToken)
        {
            if (roleId == null)
                return false;
            var role = await userRoleRepository.GetAsync(r => r.Id == roleId, cancellationToken);
            return string.Equals(role?.Name, SystemRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<HashSet<string>> GetRolePermissionsAsync(Guid? roleId, CancellationToken cancellationToken)
        {
            if (roleId == null)
                return [];
            var permissions = await userRolePermissionRepository.GetAllAsync(p => p.UserRoleId == roleId, cancellationToken);
            return permissions.Select(p => p.Permission).ToHashSet(StringComparer.Ordinal);
        }
    }
}

using Application.Exceptions;
using Domain.Constants;
using Application.Authorization;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices
{
    public class UserService(
                           IUserRepository repository,
                           IPermissionService permissionService,
                           IMapper mapper,
                           IHashService hashService,
                           IRefreshTokenRepository refreshTokenRepository,
                           ICurrentUserService currentUserService,
                           IUnitOfWork unitOfWork,
                           IUserAccessGuard accessGuard,
                           IUserRoleRepository userRoleRepository)
        : GenericEntityService<User,
                               CreateUserDto,
                               UpdateUserDto,
                               UserViewModel>(repository, permissionService, mapper), IUserService
    {
        private readonly IUserRepository _userRepository = repository;
        private readonly IHashService _hashService = hashService;
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserAccessGuard _accessGuard = accessGuard;
        private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

        /// <summary>
        /// Soft-deletes the user and ends all their sessions; a still valid access token stops working once it expires.
        /// </summary>
        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to delete this entity.");

            var target = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");
            await _accessGuard.EnsureCanDeleteUserAsync(target, cancellationToken);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsureNotLastSuperAdminAsync(target, cancellationToken);
                var deleted = await _repository.DeleteAsync(target, cancellationToken);
                await _refreshTokenRepository.RevokeAllActiveAsync(id, DateTime.UtcNow, cancellationToken);
                return ResponseModel<bool>.Ok(deleted);
            }, cancellationToken);
        }

        /// <summary>
        /// Must run inside the transaction that removes the user or their role. The SUPER_ADMIN role row is locked
        /// exclusively first, so two administrators removing each other at the same moment are serialized and the
        /// second one sees that nobody else is left.
        /// </summary>
        private async Task EnsureNotLastSuperAdminAsync(User target, CancellationToken cancellationToken)
        {
            if (target.UserRoleId is not { } roleId)
                return;
            var role = await _userRoleRepository.GetAsync(r => r.Id == roleId, cancellationToken);
            if (!string.Equals(role?.Name, SystemRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                return;

            await _userRoleRepository.LockAsync(roleId, exclusive: true, cancellationToken);
            if (!await _userRepository.AnyAsync(u => u.Id != target.Id && u.UserRoleId == roleId, cancellationToken))
                throw new ForbiddenException("The last SUPER_ADMIN cannot be removed.");
        }

        /// <summary>
        /// Gives the user a role. The services check permissions against the database, so the change applies at once;
        /// the permission claims in the user's access token follow at the next refresh.
        /// </summary>
        public async Task<ResponseModel<UserViewModel>> AssignRoleAsync(Guid userId, Guid userRoleId, CancellationToken cancellationToken = default)
        {
            if (!await _permissionService.Check(nameof(Domain.Enums.Actions.UserRoleActions.ASSIGN_USERROLE)))
                throw new ForbiddenException("You do not have permission to assign roles.");

            var target = await _repository.GetAsync(x => x.Id == userId, cancellationToken)
                                ?? throw new NotFoundException("User not found");
            if (!await _userRoleRepository.AnyAsync(r => r.Id == userRoleId, cancellationToken))
                throw new NotFoundException("Role not found");

            await _accessGuard.EnsureCanAssignRoleAsync(target, userRoleId, cancellationToken);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                if (target.UserRoleId != userRoleId)
                    await EnsureNotLastSuperAdminAsync(target, cancellationToken);

                // Shared lock: a delete of this role waits until the user has it (and then refuses with 409).
                if (!await _userRoleRepository.LockAsync(userRoleId, exclusive: false, cancellationToken))
                    throw new NotFoundException("Role not found");

                target.UserRoleId = userRoleId;
                var result = await _repository.UpdateAsync(target, cancellationToken);
                return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
            }, cancellationToken);
        }

        public override async Task<ResponseModel<UserViewModel>> CreateAsync(CreateUserDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            // Before the duplicate check: otherwise 409 vs 403 would tell a caller who may not set a PINFL whether it is registered.
            if (!string.IsNullOrWhiteSpace(entityCreateDto.INNPINFL))
                await _accessGuard.EnsureCanSetIdentifierAsync(cancellationToken);
            if (SystemRoles.IsReservedLogin(entityCreateDto.Login))
                throw new BadRequestException("This login is reserved.");

            if (await _userRepository.ExistsWithAnyAsync(entityCreateDto.Login, entityCreateDto.Email, entityCreateDto.INNPINFL, entityCreateDto.Phone, cancellationToken: cancellationToken))
                throw new ConflictException("User with this login, email, INNPINFL or phone already exists.");

            var entity = _mapper.Map<User>(entityCreateDto);
            entity.PasswordHash = _hashService.Hash(entityCreateDto.Password);
            // Same starting role as self-registration; a different one is given with PUT api/User/role.
            entity.UserRoleId = (await _userRoleRepository.GetAsync(r => r.Name == SystemRoles.DefaultUser, cancellationToken))?.Id;

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
        }

        public override async Task<ResponseModel<UserViewModel>> UpdateAsync(UpdateUserDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to update this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");
            await _accessGuard.EnsureCanManageUserAsync(entity, cancellationToken);

            var dto = entityUpdateDto;
            var unknown = dto.ClearFields.Where(f => !UpdateUserDto.ClearableFields.Contains(f)).ToList();
            if (unknown.Count > 0)
                throw new BadRequestException($"ClearFields: '{string.Join("', '", unknown)}' cannot be cleared. Allowed: {string.Join(", ", UpdateUserDto.ClearableFields)}.");
            bool Clear(string field) => dto.ClearFields.Contains(field, StringComparer.OrdinalIgnoreCase);

            // Own credentials change only through api/auth/me/*, which need the current password or a confirmation link;
            // otherwise a stolen access token of an admin would be enough to take the admin account over.
            if (entity.Id == _currentUserService.UserId
                && (!string.IsNullOrWhiteSpace(dto.Password)
                    || (dto.Email != null && !string.Equals(dto.Email.Trim(), entity.Email, StringComparison.OrdinalIgnoreCase))
                    || (dto.Login != null && !string.Equals(dto.Login, entity.Login, StringComparison.Ordinal))
                    || Clear(nameof(UpdateUserDto.Email)) || Clear(nameof(UpdateUserDto.Login))))
                throw new ForbiddenException("Change your own password or email with PUT api/auth/me/password and PUT api/auth/me/email; your login can be changed by another administrator.");

            if (dto.Login != null && !string.Equals(dto.Login, entity.Login, StringComparison.Ordinal) && SystemRoles.IsReservedLogin(dto.Login))
                throw new BadRequestException("This login is reserved.");

            // Changing or removing the PINFL/INN would move someone's E-IMZO sign-in to or away from this account.
            var newIdentifier = Clear(nameof(UpdateUserDto.INNPINFL)) ? null : dto.INNPINFL ?? entity.INNPINFL;
            if (!string.Equals(entity.INNPINFL, newIdentifier, StringComparison.Ordinal))
                await _accessGuard.EnsureCanSetIdentifierAsync(cancellationToken);

            if (await _userRepository.ExistsWithAnyAsync(dto.Login, dto.Email, dto.INNPINFL, dto.Phone, dto.Id, cancellationToken))
                throw new ConflictException("User with this login, email, INNPINFL or phone already exists.");

            var oldEmail = entity.Email;
            ApplyUpdate(dto, entity, Clear);

            // A new address has not been proven to belong to the user.
            if (!string.Equals(oldEmail, entity.Email, StringComparison.OrdinalIgnoreCase))
                entity.EmailConfirmedAt = null;

            // An empty password means "keep the current one".
            var passwordChanged = !string.IsNullOrWhiteSpace(dto.Password);
            if (passwordChanged)
                entity.PasswordHash = _hashService.Hash(dto.Password!);

            // Saving the new password and ending the sessions opened with the old one commit together.
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var result = await _repository.UpdateAsync(entity, cancellationToken)
                                    ?? throw new InvalidOperationException("Failed to update entity.");
                if (passwordChanged)
                    await _refreshTokenRepository.RevokeAllActiveAsync(entity.Id, DateTime.UtcNow, cancellationToken);

                return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
            }, cancellationToken);
        }

        /// <summary>
        /// Sent values replace the stored ones; fields not sent stay; <see cref="UpdateUserDto.ClearFields"/> removes.
        /// </summary>
        private static void ApplyUpdate(UpdateUserDto dto, User entity, Func<string, bool> clear)
        {
            if (dto.Type != null)
                entity.Type = dto.Type.Value;

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                entity.FullName = dto.FullName.Trim();
            else if (dto.FirstName != null || dto.LastName != null || dto.MiddleName != null)
            {
                if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                    throw new BadRequestException("To change the name from its parts send both LastName and FirstName, or send FullName.");
                entity.FullName = Mappings.UserMappingProfile.BuildFullName(dto.LastName, dto.FirstName, dto.MiddleName);
            }

            entity.Login = clear(nameof(dto.Login)) ? null : dto.Login ?? entity.Login;
            entity.LegalName = clear(nameof(dto.LegalName)) ? null : dto.LegalName ?? entity.LegalName;
            entity.INNPINFL = clear(nameof(dto.INNPINFL)) ? null : dto.INNPINFL ?? entity.INNPINFL;
            entity.Phone = clear(nameof(dto.Phone)) ? null : dto.Phone ?? entity.Phone;
            entity.SecondaryPhone = clear(nameof(dto.SecondaryPhone)) ? null : dto.SecondaryPhone ?? entity.SecondaryPhone;
            entity.Address = clear(nameof(dto.Address)) ? null : dto.Address ?? entity.Address;
            entity.Email = clear(nameof(dto.Email)) ? null : dto.Email ?? entity.Email;
        }

        /// <summary>
        /// The signed-in user; every user may read their own profile, so no permission is required.
        /// </summary>
        public Task<User?> GetCurrentUser(CancellationToken cancellationToken = default)
            => _currentUserService.GetCurrentUserAsync(cancellationToken);
    }
}

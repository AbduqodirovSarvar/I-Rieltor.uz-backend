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
                throw new UnauthorizedAccessException("You do not have permission to delete this entity.");

            var target = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");
            await _accessGuard.EnsureCanDeleteUserAsync(target, cancellationToken);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var deleted = await _repository.DeleteAsync(target, cancellationToken);
                await _refreshTokenRepository.RevokeAllActiveAsync(id, DateTime.UtcNow, cancellationToken);
                return ResponseModel<bool>.Ok(deleted);
            }, cancellationToken);
        }

        /// <summary>
        /// Gives the user a role. The services check permissions against the database, so the change applies at once;
        /// the permission claims in the user's access token follow at the next refresh.
        /// </summary>
        public async Task<ResponseModel<UserViewModel>> AssignRoleAsync(Guid userId, Guid userRoleId, CancellationToken cancellationToken = default)
        {
            if (!await _permissionService.Check(nameof(Domain.Enums.Actions.UserRoleActions.ASSIGN_USERROLE)))
                throw new UnauthorizedAccessException("You do not have permission to assign roles.");

            var target = await _repository.GetAsync(x => x.Id == userId, cancellationToken)
                                ?? throw new KeyNotFoundException("User not found");
            if (!await _userRoleRepository.AnyAsync(r => r.Id == userRoleId, cancellationToken))
                throw new KeyNotFoundException("Role not found");

            await _accessGuard.EnsureCanAssignRoleAsync(target, userRoleId, cancellationToken);

            target.UserRoleId = userRoleId;
            var result = await _repository.UpdateAsync(target, cancellationToken);
            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
        }

        public override async Task<ResponseModel<UserViewModel>> CreateAsync(CreateUserDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            if (await _userRepository.ExistsWithAnyAsync(entityCreateDto.Login, entityCreateDto.Email, entityCreateDto.INNPINFL, entityCreateDto.Phone, cancellationToken: cancellationToken))
                throw new InvalidOperationException("User with this login, email, INNPINFL or phone already exists.");

            if (!string.IsNullOrWhiteSpace(entityCreateDto.INNPINFL))
                await _accessGuard.EnsureCanSetIdentifierAsync(cancellationToken);

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
                throw new UnauthorizedAccessException("You do not have permission to update this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");
            await _accessGuard.EnsureCanManageUserAsync(entity, cancellationToken);

            // Changing the PINFL/INN would send someone else's E-IMZO sign-in to this account.
            if (!string.Equals(entity.INNPINFL ?? "", entityUpdateDto.INNPINFL ?? "", StringComparison.Ordinal))
                await _accessGuard.EnsureCanSetIdentifierAsync(cancellationToken);

            if (await _userRepository.ExistsWithAnyAsync(entityUpdateDto.Login, entityUpdateDto.Email, entityUpdateDto.INNPINFL, entityUpdateDto.Phone, entityUpdateDto.Id, cancellationToken))
                throw new InvalidOperationException("User with this login, email, INNPINFL or phone already exists.");

            var oldEmail = entity.Email;
            _mapper.Map(entityUpdateDto, entity);

            // A new address has not been proven to belong to the user.
            if (!string.Equals(oldEmail?.Trim(), entity.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                entity.EmailConfirmedAt = null;

            // An empty password means "keep the current one".
            var passwordChanged = !string.IsNullOrWhiteSpace(entityUpdateDto.Password);
            if (passwordChanged)
                entity.PasswordHash = _hashService.Hash(entityUpdateDto.Password!);

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            // Sessions opened with the old password end with it.
            if (passwordChanged)
                await _refreshTokenRepository.RevokeAllActiveAsync(entity.Id, DateTime.UtcNow, cancellationToken);

            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
        }

        /// <summary>
        /// The signed-in user; every user may read their own profile, so no permission is required.
        /// </summary>
        public Task<User?> GetCurrentUser(CancellationToken cancellationToken = default)
            => _currentUserService.GetCurrentUserAsync(cancellationToken);
    }
}

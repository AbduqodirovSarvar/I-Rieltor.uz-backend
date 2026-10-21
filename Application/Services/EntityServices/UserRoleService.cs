using Application.Authorization;
using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.UserRoles;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices
{
    public class UserRoleService(
                           IUserRoleRepository repository,
                           IPermissionService permissionService,
                           IMapper mapper,
                           IUserRepository userRepository,
                           IUserRolePermissionRepository userRolePermissionRepository,
                           IUnitOfWork unitOfWork,
                           IUserAccessGuard accessGuard)
        : GenericEntityService<UserRole,
                               CreateUserRoleDto,
                               UpdateUserRoleDto,
                               UserRoleViewModel>(repository, permissionService, mapper), IUserRoleService
    {
        private readonly IUserRoleRepository _roleRepository = repository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUserRolePermissionRepository _userRolePermissionRepository = userRolePermissionRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserAccessGuard _accessGuard = accessGuard;

        public override async Task<ResponseModel<UserRoleViewModel>> CreateAsync(CreateUserRoleDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            // " SUPER_ADMIN" would be stored as a different name that looks like the system role.
            entityCreateDto.Name = entityCreateDto.Name.Trim();
            await EnsureNameAllowedAsync(entityCreateDto.Name, null, cancellationToken);

            var entity = _mapper.Map<UserRole>(entityCreateDto);
            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<UserRoleViewModel>.Ok(_mapper.Map<UserRoleViewModel>(result));
        }

        /// <summary>
        /// System role names are reserved in any letter case ("super_admin" would look like a system role, and
        /// <see cref="SystemRoles.IsSystemRole"/> would then refuse to rename or delete it). Names are unique case-insensitively.
        /// </summary>
        private async Task EnsureNameAllowedAsync(string name, UserRole? existing, CancellationToken cancellationToken)
        {
            var renamingSystemRole = existing != null && SystemRoles.IsSystemRole(existing.Name);
            if (!renamingSystemRole && SystemRoles.IsSystemRole(name))
                throw new BadRequestException($"The name '{name}' is reserved for a system role.");

            var lower = name.Trim().ToLowerInvariant();
            if (await _repository.AnyAsync(r => r.Name.ToLower() == lower && (existing == null || r.Id != existing.Id), cancellationToken))
                throw new ConflictException($"A role named '{name}' already exists.");
        }

        public override async Task<ResponseModel<UserRoleViewModel>> UpdateAsync(UpdateUserRoleDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to update this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");
            await _accessGuard.EnsureCanManageRoleAsync(entity.Id, cancellationToken);

            entityUpdateDto.Name = entityUpdateDto.Name.Trim();
            // Registration and the seeder look system roles up by name.
            if (SystemRoles.IsSystemRole(entity.Name) && !string.Equals(entity.Name, entityUpdateDto.Name, StringComparison.Ordinal))
                throw new BadRequestException($"System role '{entity.Name}' cannot be renamed.");
            await EnsureNameAllowedAsync(entityUpdateDto.Name, entity, cancellationToken);

            _mapper.Map(entityUpdateDto, entity);

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<UserRoleViewModel>.Ok(_mapper.Map<UserRoleViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to delete this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");

            if (SystemRoles.IsSystemRole(entity.Name))
                throw new BadRequestException($"System role '{entity.Name}' cannot be deleted.");
            await _accessGuard.EnsureCanManageRoleAsync(entity.Id, cancellationToken);

            // The role and its permissions go together; the database no longer cascades.
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                // Locked first, so a user cannot be given the role between this check and the delete.
                if (!await _roleRepository.LockAsync(id, exclusive: true, cancellationToken))
                    throw new NotFoundException("Entity not found");

                // Soft delete keeps the row, so the FK no longer stops this; deleted users do not count.
                if (await _userRepository.AnyAsync(u => u.UserRoleId == id, cancellationToken))
                    throw new ConflictException("The role is still assigned to users.");

                foreach (var permission in await _userRolePermissionRepository.GetAllAsync(p => p.UserRoleId == id, cancellationToken))
                    await _userRolePermissionRepository.DeleteAsync(permission, cancellationToken);

                return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
            }, cancellationToken);
        }

        /// <summary>
        /// Role with its permissions, so an admin screen can show and edit them.
        /// </summary>
        public override async Task<ResponseModel<UserRoleViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to read this entity.");

            var entity = await _repository.Query(x => x.Id == id, cancellationToken)
                                .Include(x => x.Permissions)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new NotFoundException("Entity not found");

            return ResponseModel<UserRoleViewModel?>.Ok(_mapper.Map<UserRoleViewModel>(entity));
        }
    }
}

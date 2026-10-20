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
                           IUnitOfWork unitOfWork)
        : GenericEntityService<UserRole,
                               CreateUserRoleDto,
                               UpdateUserRoleDto,
                               UserRoleViewModel>(repository, permissionService, mapper), IUserRoleService
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUserRolePermissionRepository _userRolePermissionRepository = userRolePermissionRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public override async Task<ResponseModel<UserRoleViewModel>> UpdateAsync(UpdateUserRoleDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to update this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            // Registration and the seeder look system roles up by name.
            if (SystemRoles.IsSystemRole(entity.Name) && !string.Equals(entity.Name, entityUpdateDto.Name, StringComparison.Ordinal))
                throw new InvalidOperationException($"System role '{entity.Name}' cannot be renamed.");

            _mapper.Map(entityUpdateDto, entity);

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<UserRoleViewModel>.Ok(_mapper.Map<UserRoleViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRole).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to delete this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            if (SystemRoles.IsSystemRole(entity.Name))
                throw new InvalidOperationException($"System role '{entity.Name}' cannot be deleted.");

            // Soft delete keeps the row, so the FK no longer stops this; deleted users do not count.
            if (await _userRepository.AnyAsync(u => u.UserRoleId == id, cancellationToken))
                throw new ConflictException("The role is still assigned to users.");

            // The role and its permissions go together; the database no longer cascades.
            return await _unitOfWork.ExecuteAsync(async () =>
            {
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
                throw new UnauthorizedAccessException("You do not have permission to read this entity.");

            var entity = await _repository.Query(x => x.Id == id, cancellationToken)
                                .Include(x => x.Permissions)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            return ResponseModel<UserRoleViewModel?>.Ok(_mapper.Map<UserRoleViewModel>(entity));
        }
    }
}

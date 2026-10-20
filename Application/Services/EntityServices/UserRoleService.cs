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
                           IMapper mapper)
        : GenericEntityService<UserRole,
                               CreateUserRoleDto,
                               UpdateUserRoleDto,
                               UserRoleViewModel>(repository, permissionService, mapper), IUserRoleService
    {
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

            // A role that still has users is refused by the database (FK) and answered with 409.
            return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
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

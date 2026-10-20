using Application.Authorization;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.UserRolePermissions;
using Application.Models.Results;
using Application.Models.ViewModels.UserRolePermisions;
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
    public class UserRolePermissionService(
                           IUserRolePermissionRepository repository,
                           IPermissionService permissionService,
                           IMapper mapper,
                           IUserRoleRepository userRoleRepository)
        : GenericEntityService<UserRolePermission,
                                CreateUserRolePermissionDto,
                                UpdateUserRolePermissionDto,
                                UserRolePermissionViewModel>(repository, permissionService, mapper), IUserRolePermissionService
    {
        private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

        /// <summary>
        /// Grants a permission to a role. Takes effect for signed-in users at their next token refresh.
        /// </summary>
        public override async Task<ResponseModel<UserRolePermissionViewModel>> CreateAsync(CreateUserRolePermissionDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UserRolePermission).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            if (!PermissionCatalog.Exists(entityCreateDto.Permission))
                throw new ArgumentException($"Unknown permission '{entityCreateDto.Permission}'. See GET api/UserRolePermission/all-permissions.");

            if (!await _userRoleRepository.AnyAsync(x => x.Id == entityCreateDto.UserRoleId, cancellationToken))
                throw new KeyNotFoundException("Role not found");

            // The same permission twice for one role is refused by the unique index (409).
            var entity = _mapper.Map<UserRolePermission>(entityCreateDto);
            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<UserRolePermissionViewModel>.Ok(_mapper.Map<UserRolePermissionViewModel>(result));
        }
    }
}

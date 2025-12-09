using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.UserRolePermissions;
using Application.Models.ViewModels.UserRolePermisions;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices
{
    public class UserRolePermissionService(IGenericRepository<UserRolePermission> repository,
                           IPermissionService permissionService,
                           IMapper mapper)
        : GenericEntityService<UserRolePermission,
                                CreateUserRolePermissionDto,
                                UpdateUserRolePermissionDto,
                                UserRolePermissionViewModel>(repository, permissionService, mapper), IUserRolePermissionService
    {
    }
}

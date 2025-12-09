using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.UserRolePermissions;
using Application.Models.ViewModels.UserRolePermisions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface IUserRolePermissionService 
        : IGenericEntityService<UserRolePermission,
                                CreateUserRolePermissionDto,
                                UpdateUserRolePermissionDto, 
                                UserRolePermissionViewModel>
    {
    }
}

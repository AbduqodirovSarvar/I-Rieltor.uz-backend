using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.UserRoles;
using Application.Models.ViewModels.UserRoles;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface IUserRoleService 
        : IGenericEntityService<UserRole, 
                                CreateUserRoleDto,
                                UpdateUserRoleDto,
                                UserRoleViewModel>
    {
    }
}

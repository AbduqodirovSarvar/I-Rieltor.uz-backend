using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.UserRoles;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using Application.Models.ViewModels.UserRoles;
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
    public class UserRoleService(
                           IUserRoleRepository repository,
                           IPermissionService permissionService,
                           IMapper mapper)
        : GenericEntityService<UserRole, 
                               CreateUserDto, 
                               UpdateUserDto, 
                               UserRoleViewModel>(repository, permissionService, mapper), IUserRoleService
    {
        public Task<ResponseModel<UserRoleViewModel>> CreateAsync(CreateUserRoleDto entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseModel<UserRoleViewModel>> UpdateAsync(UpdateUserRoleDto entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

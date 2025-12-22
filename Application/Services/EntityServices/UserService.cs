using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.Users;
using Application.Models.ViewModels.Users;
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
    public class UserService(
                           IUserRepository repository, 
                           IPermissionService permissionService, 
                           IMapper mapper)
        : GenericEntityService<User, 
                               CreateUserDto, 
                               UpdateUserDto, 
                               UserViewModel>(repository, permissionService, mapper), IUserService
    {
        public Task<User?> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

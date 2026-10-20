using Application.Models.Results;
using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Users;
using Application.Models.ViewModels.Users;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface IUserService
        : IGenericEntityService<User,
                                CreateUserDto,
                                UpdateUserDto,
                                UserViewModel>
    {
        Task<User?> GetCurrentUser(CancellationToken cancellationToken = default);
        Task<ResponseModel<UserViewModel>> AssignRoleAsync(Guid userId, Guid userRoleId, CancellationToken cancellationToken = default);
    }
}

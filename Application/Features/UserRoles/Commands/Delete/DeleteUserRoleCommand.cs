using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UserRoles.Commands.Delete
{
    public record DeleteUserRoleCommand
        : DeleteUserDto,
        ICommand<ResponseModel<bool>>
    {
    }
}

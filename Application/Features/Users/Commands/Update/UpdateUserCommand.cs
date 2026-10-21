using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Commands.Update
{
    public record UpdateUserCommand
        : UpdateUserDto,
        ICommand<ResponseModel<UserViewModel>>
    {
    }
}

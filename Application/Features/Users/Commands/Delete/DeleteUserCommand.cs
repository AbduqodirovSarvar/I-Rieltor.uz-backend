using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Application.Features.Users.Commands.Delete
{
    public record DeleteUserCommand 
        : DeleteUserDto, 
        ICommand<ResponseModel<bool>>
    {
    }
}

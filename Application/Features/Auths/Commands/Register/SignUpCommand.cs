using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Application.Features.Auths.Commands.Register
{
    public record SignUpCommand
        : RegisterDto,
        ICommand<ResponseModel<bool>>
    {
    }
}

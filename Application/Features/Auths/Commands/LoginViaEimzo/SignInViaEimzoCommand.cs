using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.LoginViaEimzo
{
    public record SignInViaEimzoCommand
        : LoginViaEimzoDto, ICommand<ResponseModel<TokenViewModel>>
    {
    }
}

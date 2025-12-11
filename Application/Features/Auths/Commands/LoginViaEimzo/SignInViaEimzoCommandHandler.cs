using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.LoginViaEimzo
{
    public class SignInViaEimzoCommandHandler(IAuthService authService)
        : ICommandHandler<SignInViaEimzoCommand, ResponseModel<TokenViewModel>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<TokenViewModel>> Handle(SignInViaEimzoCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginViaEimzoAsync(request, cancellationToken);
            return ResponseModel<TokenViewModel>.Ok(result, "Login successful.");
        }
    }
}

using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.Login
{
    public class SignInCommandHandler(IAuthService authService) : ICommandHandler<SignInCommand, ResponseModel<TokenViewModel>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<TokenViewModel>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return ResponseModel<TokenViewModel>.Ok(result, "Login successful.");
        }
    }
}

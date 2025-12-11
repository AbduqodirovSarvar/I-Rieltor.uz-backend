using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.Register
{
    public class SignUpCommandHandler(IAuthService authService) : ICommandHandler<SignUpCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(SignUpCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Registration successful.");
        }
    }
}

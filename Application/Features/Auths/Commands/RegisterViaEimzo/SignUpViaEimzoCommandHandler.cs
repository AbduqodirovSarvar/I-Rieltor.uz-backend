using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.RegisterViaEimzo
{
    public class SignUpViaEimzoCommandHandler(IAuthService authService)
        : ICommandHandler<SignUpViaEimzoCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(SignUpViaEimzoCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterViaEimzoAsync(request, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Register successful.");
        }
    }
}

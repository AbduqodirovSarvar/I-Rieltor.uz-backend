using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Commands.ResetViaEimzo
{
    public class ResetPasswordViaEimzoCommandHandler(IAuthService authService)
        : ICommandHandler<ResetPasswordViaEimzoCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(ResetPasswordViaEimzoCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResetViaEimzoAsync(request, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Reset password successful.");
        }
    }
}

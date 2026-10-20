using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;

namespace Application.Features.Auths.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler(IAuthService authService)
        : ICommandHandler<RefreshTokenCommand, ResponseModel<TokenViewModel>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<TokenViewModel>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return ResponseModel<TokenViewModel>.Ok(result, "Token refreshed.");
        }
    }
}

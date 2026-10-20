using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;

namespace Application.Features.Auths.Commands.Logout
{
    public class LogoutCommandHandler(IAuthService authService)
        : ICommandHandler<LogoutCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LogOutAsync(request.RefreshToken, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Signed out.");
        }
    }
}

using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;

namespace Application.Features.Auths.Commands.EmailConfirmation
{
    public class ChangeMyEmailCommandHandler(IAuthService authService)
        : ICommandHandler<ChangeMyEmailCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(ChangeMyEmailCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.ChangeMyEmailAsync(request.Email, cancellationToken);
            return ResponseModel<bool>.Ok(result, "A confirmation link has been sent to the email.");
        }
    }

    public class ResendEmailConfirmationCommandHandler(IAuthService authService)
        : ICommandHandler<ResendEmailConfirmationCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResendEmailConfirmationAsync(cancellationToken);
            return ResponseModel<bool>.Ok(result, "A confirmation link has been sent to the email.");
        }
    }

    public class ConfirmEmailCommandHandler(IAuthService authService)
        : ICommandHandler<ConfirmEmailCommand, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.ConfirmEmailAsync(request.Token, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Email confirmed.");
        }
    }
}

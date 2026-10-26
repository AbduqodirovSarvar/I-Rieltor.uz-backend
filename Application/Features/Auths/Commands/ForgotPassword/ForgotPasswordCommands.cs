using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auths.Commands.ForgotPassword
{
    /// <summary>
    /// Asks for a password-reset link by email. The answer is the same whether or not the address belongs to an account, so the
    /// form cannot be used to find out who is registered. Only a confirmed email address receives a link.
    /// </summary>
    public record ForgotPasswordCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; init; } = string.Empty;
    }

    /// <summary>The token from the emailed link and the new password.</summary>
    public record ResetPasswordCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [MaxLength(1024)]
        public string Token { get; init; } = string.Empty;

        [Required]
        [PasswordValidation]
        public string NewPassword { get; init; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    public class ForgotPasswordCommandHandler(IAuthService authService)
        : ICommandHandler<ForgotPasswordCommand, ResponseModel<bool>>
    {
        public async Task<ResponseModel<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            await authService.ForgotPasswordAsync(request.Email, cancellationToken);
            return ResponseModel<bool>.Ok(true, "If this email belongs to an account, a reset link has been sent to it.");
        }
    }

    public class ResetPasswordCommandHandler(IAuthService authService)
        : ICommandHandler<ResetPasswordCommand, ResponseModel<bool>>
    {
        public async Task<ResponseModel<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var result = await authService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
            return ResponseModel<bool>.Ok(result, "Password changed. Sign in with the new password.");
        }
    }
}

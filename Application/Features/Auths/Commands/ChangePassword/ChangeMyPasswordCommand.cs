using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auths.Commands.ChangePassword
{
    /// <summary>
    /// The signed-in user changes their password. Every session ends; the caller gets a new token pair and stays signed in.
    /// </summary>
    public record ChangeMyPasswordCommand : ICommand<ResponseModel<TokenViewModel>>
    {
        [Required]
        [MaxLength(128)]
        public string CurrentPassword { get; init; } = string.Empty;

        [Required]
        [PasswordValidation]
        [MaxLength(128)]
        public string NewPassword { get; init; } = string.Empty;
    }

    public class ChangeMyPasswordCommandHandler(IAuthService authService)
        : ICommandHandler<ChangeMyPasswordCommand, ResponseModel<TokenViewModel>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<TokenViewModel>> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.ChangeMyPasswordAsync(request.CurrentPassword, request.NewPassword, cancellationToken);
            return ResponseModel<TokenViewModel>.Ok(result, "Password changed. Other devices have been signed out.");
        }
    }
}

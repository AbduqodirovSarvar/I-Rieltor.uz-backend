using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Results;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auths.Commands.EmailConfirmation
{
    /// <summary>
    /// Sets or changes the signed-in user's email (profile settings) and sends a confirmation link to it.
    /// </summary>
    public record ChangeMyEmailCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; init; } = string.Empty;
    }

    /// <summary>
    /// Sends the confirmation link again to the signed-in user's unconfirmed email.
    /// </summary>
    public record ResendEmailConfirmationCommand : ICommand<ResponseModel<bool>>
    {
    }

    /// <summary>
    /// Token from the confirmation link, posted by the frontend page the link opens.
    /// </summary>
    public record ConfirmEmailCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [MaxLength(1024)]
        public string Token { get; init; } = string.Empty;
    }
}

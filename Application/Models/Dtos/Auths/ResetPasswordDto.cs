using Domain.Behaviours;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// DTO used for resetting a user's login credentials (password)
    /// through an email confirmation code.
    /// </summary>
    public record ResetPasswordDto
    {
        /// <summary>
        /// The email address of the user requesting a password reset.
        /// This email must match an existing user account.
        /// </summary>
        [EmailAddress]
        public string Email { get; init; } = null!;

        /// <summary>
        /// The new password the user wants to set.
        /// Must meet the system's password complexity requirements.
        /// </summary>
        [PasswordValidation]
        public string Password { get; init; } = null!;

        /// <summary>
        /// Confirmation of the new password.
        /// Must be identical to the <see cref="Password"/>.
        /// </summary>
        [PasswordValidation]
        public string ConfirmPassword { get; init; } = null!;

        /// <summary>
        /// Confirmation code sent to the user's email.
        /// Used to verify the user's identity before allowing password reset.
        /// </summary>
        public string ConfirmationCode { get; init; } = null!;
    }
}

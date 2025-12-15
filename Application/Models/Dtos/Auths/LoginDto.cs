using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// DTO used for logging into the system with a standard username/email
    /// and password authentication method.
    /// </summary>
    public record LoginDto
    {
        /// <summary>
        /// The username or email address or innpinfl used for login.
        /// This field is required and must match an existing user account.
        /// </summary>
        [Required(ErrorMessage = "Login is required.")]
        public string Login { get; init; } = null!;

        /// <summary>
        /// The user's password.
        /// Must satisfy the system's password validation rules.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [PasswordValidation]
        public string Password { get; init; } = null!;
    }
}

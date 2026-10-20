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
        /// The user's password. Not checked against the password policy here:
        /// sign-in only compares it with the stored hash.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; init; } = null!;
    }
}

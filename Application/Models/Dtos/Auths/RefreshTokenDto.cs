using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// Refresh token received at sign-in or at the previous refresh. Each one works only once.
    /// </summary>
    public record RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; init; } = null!;
    }
}

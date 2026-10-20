using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// DTO used for authentication via E-Imzo digital signature.
    /// User signs a challenge string, and the signed data is validated
    /// through the E-Imzo cryptographic verification service.
    /// </summary>
    public record LoginViaEimzoDto
    {
        /// <summary>
        /// Base64 PKCS#7 of the challenge from GET api/auth/eimzo/challenge, signed with the user's E-Imzo key.
        /// The challenge is single-use and expires, so a captured signature cannot be replayed.
        /// </summary>
        [Required(ErrorMessage = "Sign with e-imzo is required.")]
        public string SignedData { get; init; } = null!;
    }
}

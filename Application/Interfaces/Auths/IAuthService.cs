using Application.Models.ViewModels.Users;
using Application.Models.Dtos.Auths;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface IAuthService
    {
        Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken);
        Task<TokenViewModel> LoginViaEimzoAsync(LoginViaEimzoDto loginDto, CancellationToken cancellationToken);
        Task<EimzoChallengeResult> GetEimzoChallengeAsync(CancellationToken cancellationToken);
        Task<bool> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken);
        Task<bool> RegisterViaEimzoAsync(RegisterViaEimzoDto registerDto, CancellationToken cancellationToken);
        Task<bool> ResetViaEimzoAsync(ResetPasswordByEImzoDto resetPasswordDto, CancellationToken cancellationToken);

        // The same three flows when the key is on the phone (E-IMZO Mobile / ID-card): the session id replaces the signed data.
        Task<TokenViewModel> LoginViaEimzoMobileAsync(string documentId, CancellationToken cancellationToken);
        Task<bool> RegisterViaEimzoMobileAsync(string documentId, CancellationToken cancellationToken);
        Task<bool> ResetViaEimzoMobileAsync(string documentId, string newPassword, CancellationToken cancellationToken);
        Task<TokenViewModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<bool> LogOutAsync(string token, CancellationToken cancellationToken);
        Task<bool> ChangeMyEmailAsync(string email, CancellationToken cancellationToken);
        Task<TokenViewModel> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);
        Task<bool> ResendEmailConfirmationAsync(CancellationToken cancellationToken);
        Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken);

        /// <summary>
        /// Links the E-IMZO key that signed the challenge to the signed-in login/password account (PINFL or INN,
        /// type, organisation name). Returns the updated profile.
        /// </summary>
        Task<UserViewModel> LinkEimzoAsync(string signedData, CancellationToken cancellationToken);

        /// <summary>
        /// Same as <see cref="LinkEimzoAsync"/> for a key whose signature is on the phone (E-IMZO Mobile session).
        /// </summary>
        Task<UserViewModel> LinkEimzoMobileAsync(string documentId, CancellationToken cancellationToken);
    }
}

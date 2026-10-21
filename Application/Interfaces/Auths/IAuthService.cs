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
        Task<TokenViewModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<bool> LogOutAsync(string token, CancellationToken cancellationToken);
        Task<bool> ChangeMyEmailAsync(string email, CancellationToken cancellationToken);
        Task<TokenViewModel> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);
        Task<bool> ResendEmailConfirmationAsync(CancellationToken cancellationToken);
        Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken);
    }
}

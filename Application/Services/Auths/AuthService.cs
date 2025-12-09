using Application.Interfaces.Auths;
using Application.Models.Dtos.Auths;
using Application.Models.ViewModels.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Auths
{
    public class AuthService : IAuthService
    {
        public Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TokenViewModel> LoginViaEimzoAsync(LoginViaEimzoDto loginDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogOutAsync(string token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TokenViewModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RegisterViaEimzoAsync(RegisterViaEimzoDto registerDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetViaEimzoAsync(ResetPasswordByEImzoDto resetPasswordDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

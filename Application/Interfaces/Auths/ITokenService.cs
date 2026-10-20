using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface ITokenService
    {
        /// <summary>
        /// Signed, short-lived JWT access token and the moment it expires (UTC).
        /// </summary>
        (string Token, DateTime ExpiresAt) GenerateAccessToken(Claim[] claims);

        /// <summary>
        /// New random refresh token for the client. Only its <see cref="HashRefreshToken"/> value may be stored.
        /// </summary>
        string GenerateRefreshToken();

        string HashRefreshToken(string refreshToken);

        TimeSpan RefreshTokenLifetime { get; }
    }
}

using Application.Interfaces.Auths;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistance.Models.ConfigModels;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Auths
{
    public class TokenService(IOptions<JWTConfiguration> config) : ITokenService
    {
        private readonly JWTConfiguration _configuration = config.Value;

        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_configuration.RefreshTokenDays);

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(Claim[] claims)
        {
            var jwtClaim = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Secret)),
                SecurityAlgorithms.HmacSha256
            );

            var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.AccessTokenMinutes);
            var token = new JwtSecurityToken(
                _configuration.ValidIssuer,
                _configuration.ValidAudience,
                claims.Concat(jwtClaim),
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return (tokenHandler.WriteToken(token), expiresAt);
        }

        public string GenerateRefreshToken()
        {
            // 512 random bits, URL-safe so it can travel in JSON or a cookie unchanged.
            return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        }

        public string HashRefreshToken(string refreshToken)
        {
            // The token is already high-entropy random data, so a fast hash without salt is enough.
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        }
    }
}

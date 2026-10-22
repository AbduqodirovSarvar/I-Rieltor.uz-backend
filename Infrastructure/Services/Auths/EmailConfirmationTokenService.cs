using Application.Exceptions;
using Application.Interfaces.Auths;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Models.ConfigModels;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.Auths
{
    /// <summary>
    /// Token = base64url(payload) + "." + base64url(HMAC-SHA256(payload)), payload = "userId|expiresUnix|email".
    /// The HMAC key is derived from the JWT secret for this purpose only (HKDF), so no new secret is needed
    /// and a confirmation token can never pass as a JWT or the other way round.
    /// </summary>
    public sealed class EmailConfirmationTokenService : IEmailConfirmationTokenService
    {
        private static readonly byte[] KeyPurpose = "i-rieltor:email-confirmation:v1"u8.ToArray();

        private readonly byte[] _key;
        private readonly EmailConfirmationConfiguration _configuration;

        public EmailConfirmationTokenService(IOptions<JWTConfiguration> jwt, IOptions<EmailConfirmationConfiguration> configuration)
        {
            _configuration = configuration.Value;
            _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(jwt.Value.Secret), 32, info: KeyPurpose);
        }

        public (string Url, DateTime ExpiresAt) CreateLink(Guid userId, string email)
        {
            // Checked here, not at startup, so a missing setting breaks only email confirmation and not sign-in.
            if (string.IsNullOrWhiteSpace(_configuration.LinkBaseUrl))
                throw new ServiceUnavailableException(
                    "EmailConfirmation:LinkBaseUrl is not configured. Set EmailConfirmation__LinkBaseUrl in .env or environment variables.");

            var expiresAt = DateTime.UtcNow.AddHours(_configuration.TokenHours);
            var payload = Encoding.UTF8.GetBytes(
                $"{userId:N}|{new DateTimeOffset(expiresAt).ToUnixTimeSeconds()}|{Normalize(email)}");
            var token = $"{Base64UrlEncoder.Encode(payload)}.{Base64UrlEncoder.Encode(Sign(payload))}";

            var separator = _configuration.LinkBaseUrl.Contains('?') ? '&' : '?';
            return ($"{_configuration.LinkBaseUrl}{separator}token={Uri.EscapeDataString(token)}", expiresAt);
        }

        public bool TryValidate(string token, out Guid userId, out string email)
        {
            userId = Guid.Empty;
            email = string.Empty;
            if (string.IsNullOrWhiteSpace(token) || token.Length > 1024)
                return false;

            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            byte[] payload, signature;
            try
            {
                payload = Base64UrlEncoder.DecodeBytes(parts[0]);
                signature = Base64UrlEncoder.DecodeBytes(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(signature, Sign(payload)))
                return false;

            var fields = Encoding.UTF8.GetString(payload).Split('|', 3);
            if (fields.Length != 3
                || !Guid.TryParseExact(fields[0], "N", out userId)
                || !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresUnix)
                || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix)
                return false;

            email = fields[2];
            return true;
        }

        public static string Normalize(string email) => email.Trim().ToLowerInvariant();

        private byte[] Sign(byte[] payload) => HMACSHA256.HashData(_key, payload);
    }
}

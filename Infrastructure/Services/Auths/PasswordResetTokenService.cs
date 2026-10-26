using Application.Exceptions;
using Application.Interfaces.Auths;
using Infrastructure.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.Auths
{
    /// <summary>
    /// Token = base64url(payload) + "." + base64url(HMAC-SHA256(payload)), payload = "userId|expiresUnix|passwordFingerprint".
    /// The key is derived from the JWT secret for this purpose only (HKDF), so a reset token can never pass as a JWT or as an
    /// email-confirmation token. The fingerprint is a hash of the stored password hash: it makes the link single-use.
    /// </summary>
    public sealed class PasswordResetTokenService : IPasswordResetTokenService
    {
        private static readonly byte[] KeyPurpose = "i-rieltor:password-reset:v1"u8.ToArray();

        private readonly byte[] _key;
        private readonly PasswordResetConfiguration _configuration;
        private readonly EmailConfirmationConfiguration _emailConfiguration;

        public PasswordResetTokenService(
            IOptions<JWTConfiguration> jwt,
            IOptions<PasswordResetConfiguration> configuration,
            IOptions<EmailConfirmationConfiguration> emailConfiguration)
        {
            _configuration = configuration.Value;
            _emailConfiguration = emailConfiguration.Value;
            _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(jwt.Value.Secret), 32, info: KeyPurpose);
        }

        public (string Url, DateTime ExpiresAt) CreateLink(Guid userId, string passwordHash)
        {
            var baseUrl = ResolveBaseUrl();
            var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.TokenMinutes);
            var payload = Encoding.UTF8.GetBytes(
                $"{userId:N}|{new DateTimeOffset(expiresAt).ToUnixTimeSeconds()}|{Fingerprint(passwordHash)}");
            var token = $"{Base64UrlEncoder.Encode(payload)}.{Base64UrlEncoder.Encode(Sign(payload))}";

            var separator = baseUrl.Contains('?') ? '&' : '?';
            return ($"{baseUrl}{separator}token={Uri.EscapeDataString(token)}", expiresAt);
        }

        public bool TryValidate(string token, out Guid userId, out string passwordFingerprint)
        {
            userId = Guid.Empty;
            passwordFingerprint = string.Empty;
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

            passwordFingerprint = fields[2];
            return true;
        }

        public string Fingerprint(string passwordHash)
            => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)), 0, 16);

        private string ResolveBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_configuration.LinkBaseUrl))
                return _configuration.LinkBaseUrl;

            const string ConfirmPage = "/confirm-email";
            var confirm = _emailConfiguration.LinkBaseUrl;
            if (!string.IsNullOrWhiteSpace(confirm) && confirm.TrimEnd('/').EndsWith(ConfirmPage, StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = confirm.TrimEnd('/');
                return trimmed[..^ConfirmPage.Length] + "/parolni-tiklash";
            }

            // Checked here, not at startup, so a missing setting breaks only password reset and not sign-in.
            throw new ServiceUnavailableException(
                "PasswordReset:LinkBaseUrl is not configured. Set PasswordReset__LinkBaseUrl in .env or environment variables.");
        }

        private byte[] Sign(byte[] payload) => HMACSHA256.HashData(_key, payload);
    }
}

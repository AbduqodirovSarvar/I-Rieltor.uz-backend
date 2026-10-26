namespace Application.Interfaces.Auths
{
    /// <summary>
    /// Signed, expiring tokens for the "reset your password" link. Nothing is stored: the token carries a fingerprint of
    /// the password it was issued against, so it works once - after the password changes (by the link or any other way) it
    /// no longer matches.
    /// </summary>
    public interface IPasswordResetTokenService
    {
        /// <summary>Link to the frontend page that reads <c>?token=</c>, and when it stops working.</summary>
        (string Url, DateTime ExpiresAt) CreateLink(Guid userId, string passwordHash);

        /// <summary>False for a forged, malformed or expired token; otherwise the user and the password fingerprint.</summary>
        bool TryValidate(string token, out Guid userId, out string passwordFingerprint);

        /// <summary>The fingerprint of a stored password hash, to compare with the one in a token.</summary>
        string Fingerprint(string passwordHash);
    }
}

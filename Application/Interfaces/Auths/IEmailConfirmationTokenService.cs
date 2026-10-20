namespace Application.Interfaces.Auths
{
    /// <summary>
    /// Signed, expiring tokens for the "confirm your email" link. Nothing is stored: the token carries
    /// the user Id and the email it was issued for, so it stops working once the user changes the email.
    /// </summary>
    public interface IEmailConfirmationTokenService
    {
        /// <summary>Link to the frontend confirmation page, with the token in the query string.</summary>
        (string Url, DateTime ExpiresAt) CreateLink(Guid userId, string email);

        /// <summary>False for a forged, malformed or expired token.</summary>
        bool TryValidate(string token, out Guid userId, out string email);
    }
}

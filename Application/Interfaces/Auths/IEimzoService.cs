using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    /// <summary>
    /// Client of the site's own E-IMZO-SERVER. The user's IP address and the site domain
    /// (X-Real-IP / Host headers required by E-IMZO-SERVER) are added by the implementation.
    /// Throws <see cref="Exceptions.ServiceUnavailableException"/> when E-IMZO-SERVER or its VPN is unreachable.
    /// </summary>
    public interface IEimzoService
    {
        /// <summary>
        /// One-time value the user signs with E-IMZO to sign in. Valid for <see cref="EimzoChallengeResult.TtlSeconds"/>.
        /// </summary>
        Task<EimzoChallengeResult> GetChallengeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Verifies a PKCS#7 signature of a challenge and returns the signer's certificate.
        /// </summary>
        Task<EimzoAuthResult> AuthenticateAsync(string signedChallengePkcs7, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a trusted timestamp to a signed document; returns the timestamped PKCS#7 (Base64).
        /// Documents should be timestamped before they are verified and stored.
        /// </summary>
        Task<string> TimestampAsync(string pkcs7Base64, CancellationToken cancellationToken);

        /// <summary>
        /// Verifies a PKCS#7 that contains the signed document itself.
        /// </summary>
        Task<EimzoDocumentVerificationResult> VerifyAttachedAsync(string pkcs7Base64, CancellationToken cancellationToken);

        /// <summary>
        /// Verifies a PKCS#7 signature of a document that is passed separately.
        /// </summary>
        Task<EimzoDocumentVerificationResult> VerifyDetachedAsync(string documentBase64, string pkcs7Base64, CancellationToken cancellationToken);
    }
}

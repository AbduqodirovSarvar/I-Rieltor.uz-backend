using Application.Interfaces.Repositories.Abstractions;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
        : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes the token only if it is still active, in one atomic statement.
        /// Returns false when another request revoked it first (two refreshes racing with the same token).
        /// </summary>
        Task<bool> TryRevokeAsync(Guid tokenId, Guid? replacedByTokenId, DateTime revokedAt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends every active session of the user (password change, user deleted).
        /// </summary>
        Task<int> RevokeAllActiveAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends one session (all tokens rotated from one sign-in) when a copy of its token is detected.
        /// </summary>
        Task<int> RevokeSessionAsync(Guid userId, Guid sessionId, DateTime revokedAt, CancellationToken cancellationToken = default);
    }
}

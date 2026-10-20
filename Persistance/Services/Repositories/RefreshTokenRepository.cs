using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class RefreshTokenRepository(AppDbContext context, IRedisService redisService)
        : GenericRepository<RefreshToken>(context, redisService), IRefreshTokenRepository
    {
        // Revocation must take effect immediately, so tokens are always read from the database.
        protected override bool UseCache => false;

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<bool> TryRevokeAsync(Guid tokenId, Guid? replacedByTokenId, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            var updated = await _dbSet
                .Where(x => x.Id == tokenId && x.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, revokedAt)
                    .SetProperty(x => x.ReplacedByTokenId, replacedByTokenId)
                    .SetProperty(x => x.UpdatedAt, revokedAt), cancellationToken);
            return updated == 1;
        }

        public async Task<int> RevokeAllActiveAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > revokedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, revokedAt)
                    .SetProperty(x => x.UpdatedAt, revokedAt), cancellationToken);
        }
    }
}

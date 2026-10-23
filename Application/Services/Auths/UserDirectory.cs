using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Auths
{
    public sealed class UserDirectory(IUserRepository users) : IUserDirectory
    {
        public async Task<IReadOnlyDictionary<Guid, UserBrief>> GetAsync(IEnumerable<Guid?> ids, CancellationToken cancellationToken = default)
        {
            var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            if (wanted.Count == 0)
                return new Dictionary<Guid, UserBrief>();
            var found = await users.Query(u => wanted.Contains(u.Id), cancellationToken)
                .Select(u => new { u.Id, u.FullName, u.Phone })
                .ToListAsync(cancellationToken);
            return found.ToDictionary(u => u.Id, u => new UserBrief(u.FullName, u.Phone));
        }
    }
}

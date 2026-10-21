using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Cachings
{
    public interface IRedisService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Writes only when the key is absent. For filling the cache on a read: a value written meanwhile after a
        /// commit (newer data) is never overwritten with what the reader loaded earlier.
        /// </summary>
        Task SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// SET NX with an expiry, for a cooldown: true when this call took the key, false when it was already there.
        /// Two parallel calls never both get true. When Redis is unavailable: true (the limit is skipped, not the request).
        /// </summary>
        Task<bool> TryAcquireAsync(string key, TimeSpan expiration);

        /// <summary>
        /// Atomic INCRBY for a counter; the expiry is set when the key has none yet (on creation).
        /// Returns the new value, 0 when Redis is unavailable.
        /// </summary>
        Task<long> IncrementAsync(string key, TimeSpan expiration, long by = 1);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}

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
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}

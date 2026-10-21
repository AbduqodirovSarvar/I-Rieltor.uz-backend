using Application.Interfaces.Cachings;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Persistance.Services.Cachings
{
    /// <summary>
    /// Redis cache. It is only an optimisation: when Redis is unavailable every call
    /// (connection error or timeout) logs a warning and behaves like a cache miss, so callers fall back to the database.
    /// </summary>
    public class RedisService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisService> logger) : IRedisService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // Tracked entities can hold navigation cycles (role -> users -> role).
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
        private readonly ILogger<RedisService> _logger = logger;

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, EXISTS {Key} skipped", key);
                return false;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                    return default;

                try
                {
                    return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
                }
                catch (Exception ex) when (ex is JsonException or NotSupportedException)
                {
                    // Written by an older version of the type (after a deploy): drop it and read from the database.
                    _logger.LogWarning(ex, "Cached value {Key} could not be read and was removed", key);
                    await RemoveAsync(key);
                    return default;
                }
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, GET {Key} skipped", key);
                return default;
            }
        }

        public async Task SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await _database.StringSetAsync(key, json, expiry, When.NotExists);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, SET NX {Key} skipped", key);
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                return await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, DEL {Key} skipped", key);
                return false;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await _database.StringSetAsync(key, json, expiry);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, SET {Key} skipped", key);
            }
        }
    }
}

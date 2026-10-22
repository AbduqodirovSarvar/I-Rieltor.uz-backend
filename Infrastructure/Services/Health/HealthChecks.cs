using Microsoft.Extensions.Diagnostics.HealthChecks;
using Persistence.Data;
using StackExchange.Redis;

namespace Infrastructure.Services.Health
{
    /// <summary>
    /// Without the database nothing works: Unhealthy (503 on /health/ready).
    /// </summary>
    public sealed class DatabaseHealthCheck(AppDbContext context) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
        {
            try
            {
                return await context.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : new HealthCheckResult(healthContext.Registration.FailureStatus, "The database is unreachable.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new HealthCheckResult(healthContext.Registration.FailureStatus, "The database is unreachable.", ex);
            }
        }
    }

    /// <summary>
    /// Redis is only a cache: registered with Degraded as its failure status, so /health/ready still answers 200
    /// and the container is not restarted while the API serves from the database.
    /// </summary>
    public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
        {
            try
            {
                await redis.GetDatabase().PingAsync();
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                return new HealthCheckResult(healthContext.Registration.FailureStatus, "Redis is unreachable; the cache is skipped.", ex);
            }
        }
    }
}

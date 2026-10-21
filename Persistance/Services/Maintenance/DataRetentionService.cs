using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistance.Data;
using Persistance.Services.Files;

namespace Persistance.Services.Maintenance
{
    /// <summary>
    /// Daily clean-up of data that must not be kept forever (personal data law ZRU-547, T-38; P3-17).
    /// Every period is a setting under "Retention" in days; 0 turns that step off:
    /// <list type="bullet">
    /// <item><c>RefreshTokenDays</c> (30): refresh tokens that expired, ended with their session or were revoked that long ago are deleted;</item>
    /// <item><c>DeletedUserDays</c> (365): soft-deleted users are anonymized (name, login, email, phones, address, PINFL/INN,
    /// password). The row stays, so records that point at it keep working;</item>
    /// <item><c>DeletedFileDays</c> (90): files in &lt;root&gt;/deleted (replaced, deleted and orphan uploads) are removed.</item>
    /// </list>
    /// <c>IntervalHours</c> (24, 0 = off) and <c>StartDelaySeconds</c> (600) set when it runs.
    /// </summary>
    public sealed class DataRetentionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DataRetentionService> logger) : BackgroundService
    {
        public const string AnonymizedName = "Deleted user";

        // The age of a file in deleted/ is counted from its move there (FileService sets the time). Files moved before
        // that was done still carry their upload time; on the first run they are given a fresh start instead of
        // being removed at once.
        public const string StartedMarker = ".retention-started";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalHours = configuration.GetValue("Retention:IntervalHours", 24.0);
            if (intervalHours <= 0)
                return;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(configuration.GetValue("Retention:StartDelaySeconds", 600)), stoppingToken);
                using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
                do
                {
                    try
                    {
                        await RunOnceAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Data retention clean-up failed; it runs again at the next interval");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
        }

        public async Task<(int RefreshTokens, int Users, int Files)> RunOnceAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tokens = 0;
            var tokenDays = configuration.GetValue("Retention:RefreshTokenDays", 30);
            if (tokenDays > 0)
            {
                var cutoff = now.AddDays(-tokenDays);
                // Hard delete (ExecuteDelete skips the soft-delete interceptor): a dead token has no value, only risk.
                tokens = await context.RefreshTokens.IgnoreQueryFilters()
                    .Where(t => t.ExpiresAt < cutoff || t.SessionExpiresAt < cutoff || (t.RevokedAt != null && t.RevokedAt < cutoff))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var users = 0;
            var userDays = configuration.GetValue("Retention:DeletedUserDays", 365);
            if (userDays > 0)
            {
                var cutoff = now.AddDays(-userDays);
                // A soft delete sets UpdatedAt, and a deleted user is never edited again.
                users = await context.Users.IgnoreQueryFilters()
                    .Where(u => u.IsDeleted && u.UpdatedAt < cutoff && u.FullName != AnonymizedName)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.FullName, AnonymizedName)
                        .SetProperty(u => u.Login, (string?)null)
                        .SetProperty(u => u.LegalName, (string?)null)
                        .SetProperty(u => u.INNPINFL, (string?)null)
                        .SetProperty(u => u.Phone, (string?)null)
                        .SetProperty(u => u.SecondaryPhone, (string?)null)
                        .SetProperty(u => u.Address, (string?)null)
                        .SetProperty(u => u.Email, (string?)null)
                        .SetProperty(u => u.EmailConfirmedAt, (DateTime?)null)
                        .SetProperty(u => u.PasswordHash, string.Empty)
                        .SetProperty(u => u.UpdatedAt, now), cancellationToken);
            }

            var files = 0;
            var fileDays = configuration.GetValue("Retention:DeletedFileDays", 90);
            var deleted = Path.Combine(FileStorageLocation.ResolveRootPath(configuration, environment.ContentRootPath), "deleted");
            if (fileDays > 0 && Directory.Exists(deleted))
                files = PurgeDeletedFiles(deleted, now.AddDays(-fileDays), now);

            if (tokens + users + files > 0)
                logger.LogInformation("Data retention: {Tokens} refresh token(s) deleted, {Users} deleted user(s) anonymized, {Files} file(s) removed from deleted/",
                    tokens, users, files);
            return (tokens, users, files);
        }

        private int PurgeDeletedFiles(string deleted, DateTime cutoff, DateTime now)
        {
            var marker = Path.Combine(deleted, StartedMarker);
            var firstRun = !File.Exists(marker);
            var removed = 0;

            foreach (var path in Directory.EnumerateFiles(deleted, "*", SearchOption.AllDirectories))
            {
                if (path == marker)
                    continue;
                try
                {
                    if (firstRun)
                        File.SetLastWriteTimeUtc(path, now);
                    else if (File.GetLastWriteTimeUtc(path) < cutoff)
                    {
                        File.Delete(path);
                        removed++;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(ex, "Deleted file {Path} could not be removed; it is tried again next run", path);
                }
            }

            if (firstRun)
                File.WriteAllText(marker, now.ToString("O"));
            return removed;
        }
    }
}

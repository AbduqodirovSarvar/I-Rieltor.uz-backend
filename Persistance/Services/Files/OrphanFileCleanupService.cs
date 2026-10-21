using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistance.Data;

namespace Persistance.Services.Files
{
    /// <summary>
    /// Moves files in &lt;root&gt;/uploads that no UploadedFiles row (deleted ones included) refers to into
    /// &lt;root&gt;/deleted/orphans. They come from a commit whose outcome was unknown (UnitOfWork keeps the file then)
    /// or from a crash between writing a file and saving its row. Moved, not deleted, so nothing is lost by mistake.
    /// FileStorage:OrphanCleanupIntervalHours (default 24, 0 = off), FileStorage:OrphanMinAgeHours (default 24: a file
    /// that young may belong to an upload still in progress).
    /// </summary>
    public sealed class OrphanFileCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<OrphanFileCleanupService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalHours = configuration.GetValue("FileStorage:OrphanCleanupIntervalHours", 24.0);
            if (intervalHours <= 0)
                return;

            // Not at startup: migrations and the seeder run first.
            var startDelay = TimeSpan.FromSeconds(configuration.GetValue("FileStorage:OrphanCleanupStartDelaySeconds", 300));
            try
            {
                await Task.Delay(startDelay, stoppingToken);
                using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
                do
                {
                    try
                    {
                        await RunOnceAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Orphan file cleanup failed; it runs again at the next interval");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
        }

        public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            var root = FileStorageLocation.ResolveRootPath(configuration, environment.ContentRootPath);
            var uploads = FileStorageLocation.UploadsPath(root);
            if (!Directory.Exists(uploads))
                return 0;

            var minAge = TimeSpan.FromHours(configuration.GetValue("FileStorage:OrphanMinAgeHours", 24.0));
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var known = (await context.UploadedFiles.IgnoreQueryFilters().AsNoTracking()
                                .Select(f => f.Name).ToListAsync(cancellationToken))
                        .ToHashSet(StringComparer.Ordinal);

            var moved = 0;
            foreach (var path in Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(path);
                if (known.Contains(name) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < minAge)
                    continue;

                // One locked or unreadable file must not stop the rest of the run.
                try
                {
                    var category = Path.GetRelativePath(uploads, Path.GetDirectoryName(path)!);
                    var target = Path.Combine(root, "deleted", "orphans", category);
                    Directory.CreateDirectory(target);
                    File.Move(path, Path.Combine(target, name), overwrite: true);
                    moved++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(ex, "Orphan upload {Path} could not be moved; it is tried again next run", path);
                }
            }

            if (moved > 0)
                logger.LogWarning("Moved {Count} orphan upload(s) without a database record to deleted/orphans", moved);
            return moved;
        }
    }
}

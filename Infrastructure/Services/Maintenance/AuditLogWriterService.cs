using Application.Interfaces.Auditing;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System.Threading.Channels;

namespace Infrastructure.Services.Maintenance
{
    /// <summary>
    /// The audit queue and the service that empties it into the AuditLogs table: batches of up to 100, as soon as there is
    /// something to write; the rest is written when the application stops. <c>Audit:Enabled</c> (default true) turns it off.
    /// </summary>
    public sealed class AuditLogWriterService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AuditLogWriterService> logger) : BackgroundService, IAuditLogQueue
    {
        private const int BatchSize = 100;
        private readonly Channel<AuditEntry> _channel = Channel.CreateBounded<AuditEntry>(
            new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
        private long _dropped;

        public void Enqueue(AuditEntry entry)
        {
            if (!configuration.GetValue("Audit:Enabled", true))
                return;
            if (!_channel.Writer.TryWrite(entry) && Interlocked.Increment(ref _dropped) % 100 == 1)
                logger.LogWarning("The audit queue is full; {Dropped} entries have been dropped so far", _dropped);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<AuditEntry>(BatchSize);
            try
            {
                while (await _channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    Fill(batch);
                    await SaveAsync(batch);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down: what is still queued is written below.
            }

            while (_channel.Reader.TryPeek(out _))
            {
                Fill(batch);
                await SaveAsync(batch);
            }
        }

        private void Fill(List<AuditEntry> batch)
        {
            batch.Clear();
            while (batch.Count < BatchSize && _channel.Reader.TryRead(out var entry))
                batch.Add(entry);
        }

        private async Task SaveAsync(List<AuditEntry> batch)
        {
            if (batch.Count == 0)
                return;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.AuditLogs.AddRange(batch.Select(e => new AuditLog
                {
                    OccurredAt = e.OccurredAt,
                    UserId = e.UserId,
                    Action = e.Action,
                    Method = e.Method,
                    Path = e.Path,
                    StatusCode = e.StatusCode,
                    IpAddress = e.IpAddress,
                    UserAgent = e.UserAgent,
                    CorrelationId = e.CorrelationId,
                    DurationMs = e.DurationMs,
                }));
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The audit trail must never take the application down; a lost batch is reported.
                logger.LogError(ex, "{Count} audit entries could not be written", batch.Count);
            }
        }
    }
}

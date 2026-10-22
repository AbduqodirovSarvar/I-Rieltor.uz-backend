namespace Application.Interfaces.Auditing
{
    /// <param name="OccurredAt">When the request arrived (UTC).</param>
    public sealed record AuditEntry(
        DateTime OccurredAt,
        Guid? UserId,
        string Action,
        string Method,
        string Path,
        int StatusCode,
        string? IpAddress,
        string? UserAgent,
        string? CorrelationId,
        int DurationMs);

    /// <summary>
    /// Takes audit entries without slowing the request: they are written to the database in batches by a background service.
    /// When the queue is full, entries are dropped (and counted in the log) rather than holding up requests.
    /// </summary>
    public interface IAuditLogQueue
    {
        void Enqueue(AuditEntry entry);
    }
}

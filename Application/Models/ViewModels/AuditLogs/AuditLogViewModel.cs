using Application.Models.ViewModels.Abstractions;

namespace Application.Models.ViewModels.AuditLogs
{
    public record AuditLogViewModel : BaseViewModel
    {
        public DateTime OccurredAt { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
        public int DurationMs { get; set; }
    }
}

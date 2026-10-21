using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Channels;

namespace WebApi.Monitoring
{
    /// <summary>
    /// The simplest error monitoring (T-37): every log event of level Error or higher is collected, and at most once per
    /// <c>Alerts:MinIntervalMinutes</c> (default 15) one email listing them goes to <c>Alerts:Email</c>, through the
    /// SMTP settings the API already has. Off while <c>Alerts:Email</c> is empty. Downtime itself is noticed by an
    /// external uptime check on /health/ready (README).
    /// </summary>
    public sealed class ErrorAlertSink : BackgroundService, ILogEventSink
    {
        private const int MaxEventsPerEmail = 20;

        // Failures of mail sending itself are not reported by mail: they would only produce more failing mail.
        // The request log line of a 5xx only repeats the error already logged by the code that failed.
        private static readonly string[] IgnoredSources =
        [
            "Persistance.Services.Notifications.EmailService",
            "Serilog.AspNetCore.RequestLoggingMiddleware",
            typeof(ErrorAlertSink).FullName!,
        ];

        private readonly Channel<LogEvent> _events = Channel.CreateBounded<LogEvent>(
            new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
        private readonly SmtpConfiguration _smtp;
        private readonly string _recipient;
        private readonly TimeSpan _interval;
        private readonly string _environment;

        public ErrorAlertSink(IConfiguration configuration, IOptions<SmtpConfiguration> smtp, IHostEnvironment environment)
        {
            _smtp = smtp.Value;
            _recipient = configuration["Alerts:Email"] ?? string.Empty;
            _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Alerts:MinIntervalMinutes", 15)));
            _environment = environment.EnvironmentName;
        }

        public bool Enabled => !string.IsNullOrWhiteSpace(_recipient);

        public void Emit(LogEvent logEvent)
        {
            if (!Enabled || logEvent.Level < LogEventLevel.Error)
                return;
            if (logEvent.Properties.TryGetValue("SourceContext", out var source)
                && IgnoredSources.Contains((source as ScalarValue)?.Value as string))
                return;
            _events.Writer.TryWrite(logEvent);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Enabled)
                return;
            try
            {
                while (await _events.Reader.WaitToReadAsync(stoppingToken))
                {
                    var batch = new List<LogEvent>();
                    var dropped = 0;
                    while (_events.Reader.TryRead(out var logEvent))
                    {
                        if (batch.Count < MaxEventsPerEmail) batch.Add(logEvent);
                        else dropped++;
                    }

                    await SendAsync(batch, dropped, stoppingToken);
                    // Errors that come meanwhile wait for the next email: one incident is one email, not hundreds.
                    await Task.Delay(_interval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
        }

        private async Task SendAsync(List<LogEvent> batch, int dropped, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.FromAddress))
                return;

            var body = new StringBuilder();
            foreach (var logEvent in batch)
            {
                body.AppendLine($"{logEvent.Timestamp.UtcDateTime:yyyy-MM-dd HH:mm:ss}Z  {logEvent.Level}  {Property(logEvent, "SourceContext")}  id={Property(logEvent, "CorrelationId")}");
                body.AppendLine(logEvent.RenderMessage());
                if (logEvent.Exception != null)
                    body.AppendLine($"{logEvent.Exception.GetType().FullName}: {logEvent.Exception.Message}");
                body.AppendLine();
            }
            if (dropped > 0)
                body.AppendLine($"... and {dropped} more; see the logs.");

            try
            {
                using var client = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    EnableSsl = _smtp.EnableSsl,
                    Credentials = string.IsNullOrEmpty(_smtp.UserName) ? null : new NetworkCredential(_smtp.UserName, _smtp.Password),
                };
                using var message = new MailMessage(new MailAddress(_smtp.FromAddress, _smtp.FromName), new MailAddress(_recipient))
                {
                    Subject = $"I-Rieltor API ({_environment}): {batch.Count + dropped} error(s)",
                    Body = body.ToString(),
                    IsBodyHtml = false,
                };
                await client.SendMailAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or IOException)
            {
                // Not through the logger: that would come back here.
                SelfLog.WriteLine("Error alert email could not be sent: {0}", ex.Message);
            }
        }

        private static string Property(LogEvent logEvent, string name)
            => logEvent.Properties.TryGetValue(name, out var value) ? (value as ScalarValue)?.Value?.ToString() ?? value.ToString() : "-";
    }
}

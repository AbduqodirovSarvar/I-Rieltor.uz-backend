using Application.Exceptions;
using Application.Interfaces.Notifications;
using Application.Notifications;
using System.Collections.Concurrent;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// Collects what the API "sends" by email and SMS, and can make either channel fail.
    /// </summary>
    public sealed class FakeNotificationSinks
    {
        public sealed record Email(string To, string Subject, string Html);
        public sealed record Sms(string To, string Text);

        public ConcurrentQueue<Email> Emails { get; } = new();
        public ConcurrentQueue<Sms> Messages { get; } = new();

        public volatile bool EmailFails;
        public volatile bool SmsFails;
        /// <summary>When set, SMS to a number fails like a number the provider refuses (not retried).</summary>
        public volatile string? SmsRefusedNumber;

        public IEnumerable<Email> EmailsTo(string address) => Emails.Where(e => e.To.Equals(address, StringComparison.OrdinalIgnoreCase));
        public IEnumerable<Sms> SmsTo(string phone) => Messages.Where(m => m.To == phone);
    }

    public sealed class FakeEmailService(FakeNotificationSinks sinks) : IEmailService
    {
        public Task SendEmailAsync(string email, string subject, HtmlEmailBody body, CancellationToken cancellationToken = default)
        {
            if (sinks.EmailFails)
                throw new ServiceUnavailableException("The email could not be sent. Try again later.");
            sinks.Emails.Enqueue(new FakeNotificationSinks.Email(email, subject, body.Html));
            return Task.CompletedTask;
        }
    }

    public sealed class FakeSmsSender(FakeNotificationSinks sinks) : ISmsSender
    {
        public bool IsEnabled => true;

        public Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
        {
            if (sinks.SmsRefusedNumber == phone)
                throw new BadRequestException("The phone number is not valid.");
            if (sinks.SmsFails)
                throw new ServiceUnavailableException("The SMS provider is unreachable.");
            sinks.Messages.Enqueue(new FakeNotificationSinks.Sms(phone, message));
            return Task.CompletedTask;
        }
    }
}

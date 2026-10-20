using Application.Notifications;

namespace Application.Interfaces.Notifications
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an HTML email. The body is an <see cref="HtmlEmailBody"/>, so user data in it is always encoded.
        /// Line breaks in the subject are replaced with spaces (they would otherwise start new mail headers).
        /// </summary>
        Task SendEmailAsync(string email, string subject, HtmlEmailBody body, CancellationToken cancellationToken = default);
    }
}

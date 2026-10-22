using Application.Exceptions;
using Application.Interfaces.Notifications;
using Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Notifications
{
    public class EmailService(IOptions<SmtpConfiguration> options, ILogger<EmailService> logger) : IEmailService
    {
        private readonly SmtpConfiguration _smtp = options.Value;
        private readonly ILogger<EmailService> _logger = logger;

        public async Task SendEmailAsync(string email, string subject, HtmlEmailBody body, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(body);
            // Like a mail server that is down: email fails (503), everything else keeps working.
            if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.FromAddress))
                throw new ServiceUnavailableException("The email could not be sent. Try again later.");

            MailAddress to;
            try
            {
                to = new MailAddress(email);
            }
            catch (FormatException)
            {
                throw new BadRequestException("Email address is not valid.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
                Subject = SingleLine(subject),
                Body = body.Html,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(to);

            using var smtp = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
                EnableSsl = _smtp.EnableSsl,
            };

            try
            {
                await smtp.SendMailAsync(message, cancellationToken);
                // Only the domain is logged: the full address is personal data.
                _logger.LogInformation("Email '{Subject}' sent to a {Domain} address", message.Subject, to.Host);
            }
            // SmtpException: refused or failed; InvalidOperationException: SMTP host not configured.
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
            {
                _logger.LogError(ex, "Sending email '{Subject}' to a {Domain} address failed", message.Subject, to.Host);
                throw new ServiceUnavailableException("The email could not be sent. Try again later.", ex);
            }
        }

        private static string SingleLine(string subject)
            => new((subject ?? string.Empty).Select(c => char.IsControl(c) ? ' ' : c).ToArray());
    }
}

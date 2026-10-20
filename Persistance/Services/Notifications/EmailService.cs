using Application.Interfaces.Notifications;
using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Notifications
{
    public class EmailService(IOptions<SmtpConfiguration> options) : IEmailService
    {
        private readonly SmtpConfiguration _smtp = options.Value;

        public async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email));

            using var smtp = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
                EnableSsl = _smtp.EnableSsl,
            };

            try
            {
                await smtp.SendMailAsync(message);
                Console.WriteLine("Message sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
            return true;
        }
    }
}

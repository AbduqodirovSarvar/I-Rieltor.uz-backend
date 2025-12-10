using Application.Interfaces.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Notifications
{
    public class EmailService : IEmailService
    {
        public async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            var model = new
            {
                Name = "Family Tree",
                Email = email,
                Message = body,
                Subject = subject,
            };

            body = $"<p>Email From: {model.Name} ({model.Email})</p><p>Message:</p><p>{model.Message}</p>";

            var message = new MailMessage();
            message.To.Add(new MailAddress(model.Email));
            message.From = new MailAddress("abduqodirovsarvar.2002@gmail.com");
            message.Subject = model.Subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("abduqodirovsarvar.2002@gmail.com", "hgjb mvzo nuji fcwb"),
                EnableSsl = true,
            };

            try
            {
                await smtp.SendMailAsync(message);
                Console.WriteLine("Message sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw new Exception(ex.Message);
            }
            return true;
        }
    }
}

using Application.Interfaces.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Notifications
{
    public class EmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            throw new NotImplementedException();
        }
    }
}

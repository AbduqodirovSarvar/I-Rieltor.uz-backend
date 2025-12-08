using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Notifications
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string email, string subject, string body);
    }
}

using Application.Exceptions;
using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Cachings;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Models.Results;
using Application.Notifications;
using Domain.Behaviours;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Contact
{
    /// <summary>
    /// A message from the contact form of the public site; it is e-mailed to the company (Contact:Email, else the company
    /// e-mail on file). Nothing is stored. Anyone may send it, so the form carries a hidden field that people leave empty
    /// (<see cref="Website"/>): a robot that fills it in gets the usual answer and nothing is sent.
    /// </summary>
    public record SendContactMessageCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; init; } = string.Empty;

        [PhoneNumberValidation]
        public string? Phone { get; init; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; init; }

        [Required]
        [MinLength(10)]
        [MaxLength(2000)]
        public string Message { get; init; } = string.Empty;

        /// <summary>The trap for robots: people never see this field, so it must stay empty.</summary>
        [MaxLength(200)]
        public string? Website { get; init; }
    }

    public class SendContactMessageCommandHandler(
        IEmailService emailService,
        ICompanyRepository companies,
        IRedisService redis,
        IConfiguration configuration)
        : ICommandHandler<SendContactMessageCommand, ResponseModel<bool>>
    {
        private const int MessagesPerDay = 5;

        public async Task<ResponseModel<bool>> Handle(SendContactMessageCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Website))
                return Sent();

            var name = Clean(request.Name);
            var phone = request.Phone?.Trim();
            var email = request.Email?.Trim();
            var message = request.Message.Trim();
            if (name.Length == 0 || message.Length < 10)
                throw new BadRequestException("Name and a message of at least 10 characters are required.");
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email))
                throw new BadRequestException("Leave a phone number or an e-mail, so that we can answer.");

            var recipient = configuration["Contact:Email"];
            if (string.IsNullOrWhiteSpace(recipient))
                recipient = (await companies.GetAsync(c => true, cancellationToken))?.Email;
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ServiceUnavailableException("The contact form is not set up yet. Call us instead.");

            // Per person (phone or e-mail), not per address of the server: the same text sent again and again is stopped.
            var who = (email ?? phone)!.ToLowerInvariant();
            if (await redis.IncrementAsync($"v1:contact:day:{who}:{DateTime.UtcNow:yyyyMMdd}", TimeSpan.FromDays(1)) > MessagesPerDay)
                throw new TooManyRequestsException("Too many messages today. Try again tomorrow.");

            var body = HtmlEmailBody.FromTemplate(EmailTemplates.ContactMessage, new Dictionary<string, string?>
            {
                ["name"] = name,
                ["phone"] = phone ?? "-",
                ["email"] = email ?? "-",
                ["message"] = message
            });
            await emailService.SendEmailAsync(recipient, EmailTemplates.ContactMessageSubject, body, cancellationToken);
            return Sent();
        }

        private static ResponseModel<bool> Sent() => ResponseModel<bool>.Ok(true, "Thank you. We will answer soon.");

        /// <summary>One line: line breaks in a name are not wanted.</summary>
        private static string Clean(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

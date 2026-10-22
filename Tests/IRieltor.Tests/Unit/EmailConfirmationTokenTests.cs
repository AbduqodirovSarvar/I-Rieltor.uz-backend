using Application.Behaviours;
using Application.Exceptions;
using Microsoft.Extensions.Options;
using Infrastructure.Models.ConfigModels;
using Infrastructure.Services.Auths;
using System.ComponentModel.DataAnnotations;

namespace IRieltor.Tests.Unit
{
    public class EmailConfirmationTokenTests
    {
        private static EmailConfirmationTokenService Create(string secret = "unit-test-secret-unit-test-secret-0123456789")
            => new(Options.Create(new JWTConfiguration { Secret = secret }),
                   Options.Create(new EmailConfirmationConfiguration { LinkBaseUrl = "https://i-rieltor.uz/confirm-email", TokenHours = 24 }));

        private static string TokenOf(string url) => Uri.UnescapeDataString(url[(url.IndexOf("token=", StringComparison.Ordinal) + 6)..]);

        [Fact]
        public void Link_round_trips()
        {
            var service = Create();
            var userId = Guid.CreateVersion7();
            var (url, expiresAt) = service.CreateLink(userId, " Ali@Example.com ");

            Assert.StartsWith("https://i-rieltor.uz/confirm-email?token=", url);
            Assert.InRange(expiresAt, DateTime.UtcNow.AddHours(23), DateTime.UtcNow.AddHours(25));
            Assert.True(service.TryValidate(TokenOf(url), out var id, out var email));
            Assert.Equal(userId, id);
            Assert.Equal("ali@example.com", email);
        }

        [Fact]
        public void Tampered_token_or_other_secret_is_refused()
        {
            var (url, _) = Create().CreateLink(Guid.CreateVersion7(), "a@example.com");
            var token = TokenOf(url);

            Assert.False(Create("another-secret-another-secret-0123456789ab").TryValidate(token, out _, out _));
            Assert.False(Create().TryValidate(token[..^2] + (token[^2] == 'A' ? "BB" : "AA"), out _, out _));
            Assert.False(Create().TryValidate("garbage", out _, out _));
        }

        private sealed record Command([property: Required, MaxLength(3)] string Name);

        [Fact]
        public async Task Validation_behavior_refuses_invalid_requests_with_field_errors()
        {
            var behavior = new DataAnnotationsValidationBehavior<Command, int>();

            var error = await Assert.ThrowsAsync<BadRequestException>(() => behavior.Handle(new Command("too long"), _ => Task.FromResult(1), default));
            Assert.Contains(error.Errors!, e => e.StartsWith("Name:"));
            Assert.Equal(1, await behavior.Handle(new Command("ok"), _ => Task.FromResult(1), default));
        }
    }
}

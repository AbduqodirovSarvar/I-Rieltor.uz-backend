using Application.Exceptions;
using Application.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using Persistance.Services.Notifications;
using System.Net;
using System.Text;

namespace IRieltor.Tests.Unit
{
    public class NotificationUnitTests
    {
        [Theory]
        [InlineData(103_000_000, "103 000 000")]
        [InlineData(5_000_000.4, "5 000 000")]
        [InlineData(999, "999")]
        [InlineData(0, "0")]
        public void Money_is_whole_som_with_space_separated_thousands(double amount, string expected)
            => Assert.Equal(expected, NotificationTemplates.Money((decimal)amount));

        [Fact]
        public void Time_is_shown_in_tashkent()
            => Assert.Equal("21.10.2026 14:00", NotificationTemplates.Time(new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc)));

        [Fact]
        public void Every_template_renders_with_exactly_its_placeholders()
        {
            foreach (var type in Enum.GetValues<Domain.Enums.NotificationType>())
            {
                var template = NotificationTemplates.Get(type);
                var keys = System.Text.RegularExpressions.Regex.Matches(template.Text, @"\{([a-zA-Z]+)\}").Select(m => m.Groups[1].Value).Distinct().ToDictionary(k => k, _ => "x");
                Assert.DoesNotContain("{", NotificationTemplates.Render(template.Text, keys));
                Assert.False(string.IsNullOrWhiteSpace(template.Subject));
            }
        }

        // ---- Eskiz

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<(string Path, string? Auth, string Body)> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Parameter, body));
                return respond(request);
            }
        }

        private static HttpResponseMessage Json(HttpStatusCode code, string json) => new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static EskizSmsSender Sender(StubHandler handler, string email = "me@x.uz", string password = "pw")
            => new(new HttpClient(handler) { BaseAddress = new Uri("https://eskiz.test") },
                   Options.Create(new EskizConfiguration { Email = email, Password = password, From = "4546" }),
                   new EskizTokenStore(), NullLogger<EskizSmsSender>.Instance);

        [Fact]
        public async Task Eskiz_logs_in_once_and_sends_the_number_without_a_plus()
        {
            var handler = new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/auth/login"
                ? Json(HttpStatusCode.OK, """{"data":{"token":"tok-1"}}""")
                : Json(HttpStatusCode.OK, """{"status":"waiting"}"""));
            var sender = Sender(handler);

            await sender.SendAsync("+998901234567", "salom");
            await sender.SendAsync("+998901234568", "salom 2");

            Assert.Equal(["/api/auth/login", "/api/message/sms/send", "/api/message/sms/send"], handler.Requests.Select(r => r.Path));
            Assert.All(handler.Requests.Skip(1), r => Assert.Equal("tok-1", r.Auth));
            Assert.Contains("998901234567", handler.Requests[1].Body);
            Assert.DoesNotContain("+998901234567", handler.Requests[1].Body);
        }

        [Fact]
        public async Task Eskiz_logs_in_again_when_the_token_has_expired()
        {
            var tokens = new Queue<string>(["old", "new"]);
            var handler = new StubHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/auth/login")
                {
                    var token = tokens.Dequeue();
                    return Json(HttpStatusCode.OK, $$"""{"data":{"token":"{{token}}" } }""");
                }
                return request.Headers.Authorization?.Parameter == "old" ? Json(HttpStatusCode.Unauthorized, "{}") : Json(HttpStatusCode.OK, "{}");
            });

            await Sender(handler).SendAsync("+998901234567", "salom");

            Assert.Equal(["/api/auth/login", "/api/message/sms/send", "/api/auth/login", "/api/message/sms/send"], handler.Requests.Select(r => r.Path));
            Assert.Equal("new", handler.Requests[^1].Auth);
        }

        [Fact]
        public async Task Eskiz_refuses_bad_numbers_and_reports_a_failing_provider()
        {
            var ok = new StubHandler(_ => Json(HttpStatusCode.OK, """{"data":{"token":"t"}}"""));
            await Assert.ThrowsAsync<BadRequestException>(() => Sender(ok).SendAsync("+12", "x"));
            await Assert.ThrowsAsync<BadRequestException>(() => Sender(ok).SendAsync("+99890abc4567", "x"));

            var down = new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/auth/login"
                ? Json(HttpStatusCode.OK, """{"data":{"token":"t"}}""")
                : Json(HttpStatusCode.InternalServerError, "{}"));
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => Sender(down).SendAsync("+998901234567", "x"));

            var noLogin = new StubHandler(_ => Json(HttpStatusCode.Unauthorized, "{}"));
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => Sender(noLogin).SendAsync("+998901234567", "x"));
        }

        [Fact]
        public async Task Eskiz_without_credentials_is_off()
        {
            var sender = Sender(new StubHandler(_ => Json(HttpStatusCode.OK, "{}")), email: "", password: "");

            Assert.False(sender.IsEnabled);
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => sender.SendAsync("+998901234567", "x"));
        }
    }
}

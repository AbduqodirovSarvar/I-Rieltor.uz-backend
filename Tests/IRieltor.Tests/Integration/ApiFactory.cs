using Application.Interfaces.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// The whole API against real PostgreSQL and Redis in Docker (Testcontainers), with migrations and the seeder.
    /// Needs a running Docker. Settings go in as environment variables before the app starts, because the app reads
    /// some of them while registering services; .env values are only used for keys not set here (DotEnvLoader).
    /// </summary>
    public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string AdminLogin = "SUPER_ADMIN";
        public const string AdminPassword = "Test-Admin#2026";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
        private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();
        private readonly string _files = Path.Combine(Path.GetTempPath(), "irieltor-tests-" + Guid.NewGuid().ToString("N"));

        public const string PaymeMerchantId = "test-merchant";
        public const string PaymeKey = "payme-test-key";
        public const string ClickServiceId = "12345";
        public const string ClickSecret = "click-test-secret";

        public FakeEimzoServer Eimzo { get; private set; } = null!;
        public FakeNotificationSinks Sinks { get; } = new();
        private static long _pinfl = 31_000_000_000_000;

        /// <summary>A 14-digit PINFL no other test uses.</summary>
        public static string NewPinfl() => Interlocked.Increment(ref _pinfl).ToString();

        public string ConnectionString => _postgres.GetConnectionString();
        public string FilesRoot => _files;

        public async ValueTask InitializeAsync()
        {
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
            Eimzo = await FakeEimzoServer.StartAsync();

            foreach (var (key, value) in new Dictionary<string, string>
            {
                ["ConnectionStrings__PostgreSqlConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings__Redis"] = _redis.GetConnectionString(),
                ["JWTConfiguration__Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                ["DefaultAdmin__Password"] = AdminPassword,
                ["DefaultAdmin__Email"] = "admin@tests.local",
                // Nothing listens there: confirmation emails fail fast (logged), no real mail leaves a test run.
                ["Smtp__Host"] = "127.0.0.1",
                ["Smtp__Port"] = "9",
                ["Smtp__EnableSsl"] = "false",
                ["Smtp__FromAddress"] = "noreply@tests.local",
                ["Eimzo__BaseUrl"] = Eimzo.BaseUrl,
                ["EmailConfirmation__LinkBaseUrl"] = "http://localhost/confirm-email",
                ["FileStorage__RootPath"] = _files,
                ["FileStorage__OrphanCleanupIntervalHours"] = "0",
                ["Retention__IntervalHours"] = "0",
                // The tests move the auctions along themselves (AuctionLifecycleService.RunOnceAsync with their own clock).
                ["Auctions__LifecycleIntervalSeconds"] = "0",
                // TestServer has no client IP: every request shares one rate-limit bucket.
                ["RateLimiting__AuthPermitPerMinute"] = "1000",
                ["RateLimiting__SessionPermitPerMinute"] = "1000",
                ["RateLimiting__BidPermitPer10Seconds"] = "1000",
                // Payment methods; the providers' secrets are the tests' own.
                ["Payments__Payme__MerchantId"] = PaymeMerchantId,
                ["Payments__Payme__Key"] = PaymeKey,
                ["Payments__Click__ServiceId"] = ClickServiceId,
                ["Payments__Click__MerchantId"] = "777",
                ["Payments__Click__SecretKey"] = ClickSecret,
                ["Payments__BankTransfer__Instructions"] = "Test Bank, hisob 20208000123456789, MFO 00014",
                ["Payments__ReturnUrl"] = "https://i-rieltor.test/payment/result",
                // One in-memory hub per test host is enough; the Redis backplane is exercised separately.
                ["SignalR__RedisBackplane"] = "false",
            })
                Environment.SetEnvironmentVariable(key, value);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // What the API would email or text goes to the sinks, so tests can read it and make the channels fail.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.RemoveAll<ISmsSender>();
                services.AddSingleton(Sinks);
                services.AddScoped<IEmailService, FakeEmailService>();
                services.AddScoped<ISmsSender, FakeSmsSender>();
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask(), Eimzo.DisposeAsync().AsTask());
            try { Directory.Delete(_files, recursive: true); } catch (IOException) { }
        }

        public async Task<string> SignInAsync(string login, string password)
        {
            var response = await CreateClient().PostAsJsonAsync("api/auth/sign-in", new { login, password });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonObject>())!["data"]!["accessToken"]!.GetValue<string>();
        }

        public async Task<HttpClient> AdminClientAsync() => Authorized(await SignInAsync(AdminLogin, AdminPassword));

        public HttpClient Authorized(string accessToken)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        public async Task SignUpAsync(string login, string password = "Strong@123")
        {
            var response = await CreateClient().PostAsJsonAsync("api/auth/sign-up",
                new { firstName = "Test", lastName = "User", login, email = $"{login}@tests.local", password });
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>One-time challenge from the API, as the browser would fetch it before signing.</summary>
        public async Task<string> ChallengeAsync()
            => (await JsonAsync(await CreateClient().GetAsync("api/auth/eimzo/challenge")))["data"]!["challenge"]!.GetValue<string>();

        /// <summary>Signs a fresh challenge with the key of <paramref name="pinfl"/> (and the organisation, for a legal-entity key).</summary>
        public async Task<string> SignedChallengeAsync(string pinfl, string? legalInn = null)
            => FakeEimzoServer.Sign(await ChallengeAsync(), pinfl, legalInn, legalInn == null ? null : "ORG MCHJ");

        /// <summary>A user who signed up with E-IMZO (an account with a verified PINFL), signed in.</summary>
        public async Task<(HttpClient Client, string UserId, string Pinfl)> EimzoUserAsync(string? legalInn = null)
        {
            var pinfl = NewPinfl();
            var register = await CreateClient().PostAsJsonAsync("api/auth/sign-up-via-eimzo", new { signedData = await SignedChallengeAsync(pinfl, legalInn) });
            Assert.Equal(System.Net.HttpStatusCode.OK, register.StatusCode);
            var identifier = legalInn ?? pinfl;
            var signIn = await CreateClient().PostAsJsonAsync("api/auth/sign-in-via-eimzo", new { signedData = await SignedChallengeAsync(pinfl, legalInn) });
            signIn.EnsureSuccessStatusCode();
            var data = (await JsonAsync(signIn))["data"]!;
            return (Authorized(data["accessToken"]!.GetValue<string>()), data["userId"]!.GetValue<string>(), identifier);
        }

        public async Task<T?> ScalarAsync<T>(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            var value = await command.ExecuteScalarAsync();
            return value is null or DBNull ? default : (T)value;
        }

        public static async Task<JsonObject> JsonAsync(HttpResponseMessage response)
            => (await response.Content.ReadFromJsonAsync<JsonObject>(JsonSerializerOptions.Web))!;
    }

    [CollectionDefinition(Name)]
    public sealed class ApiCollection : ICollectionFixture<ApiFactory>
    {
        public const string Name = "api";
    }
}

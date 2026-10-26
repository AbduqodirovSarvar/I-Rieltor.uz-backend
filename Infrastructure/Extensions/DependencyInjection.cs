using Application.Authorization;
using Application.Interfaces.Auditing;
using Application.Interfaces.Auths;
using Application.Interfaces.Cachings;
using Application.Interfaces.Files;
using Application.Interfaces.Notifications;
using Infrastructure.Models.ConfigModels;
using Infrastructure.Services.Auths;
using Infrastructure.Services.Cachings;
using Infrastructure.Services.Files;
using Infrastructure.Services.Health;
using Infrastructure.Services.Maintenance;
using Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Everything that talks to the outside world or runs in the background: sign-in (JWT, E-IMZO), Redis, files, email and SMS,
    /// health checks and the maintenance services. The database itself is in Persistence.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<ITokenService, TokenService>();
            // A typo in a number (e.g. TokenHours=0) stops the app at start, not at the first email or sign-in.
            // Missing strings (SMTP host, link base URL) are still reported when used, so one gap breaks only that feature.
            services.AddOptions<EmailConfirmationConfiguration>().Bind(configuration.GetSection(EmailConfirmationConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddSingleton<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
            services.AddOptions<PasswordResetConfiguration>().Bind(configuration.GetSection(PasswordResetConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddSingleton<IPasswordResetTokenService, PasswordResetTokenService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddOptions<EimzoConfiguration>().Bind(configuration.GetSection(EimzoConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddHttpClient<IEimzoService, EimzoService>((serviceProvider, client) =>
            {
                var eimzo = serviceProvider.GetRequiredService<IOptions<EimzoConfiguration>>().Value;
                client.BaseAddress = new Uri(eimzo.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(eimzo.TimeoutSeconds);
            });

            services.AddScoped<IFileService>(provider =>
            {
                var env = provider.GetRequiredService<IHostEnvironment>();
                return new FileService(
                    FileStorageLocation.ResolveRootPath(configuration, env.ContentRootPath),
                    configuration["FileStorage:PublicBaseUrl"],
                    configuration.GetValue("FileStorage:MaxFileSizeMb", 20));
            });

            services.AddScoped<IPrivateFileStore>(provider =>
                new PrivateFileStore(
                    FileStorageLocation.ResolveRootPath(configuration, provider.GetRequiredService<IHostEnvironment>().ContentRootPath),
                    configuration.GetValue("PrivateFiles:MaxFileSizeMb", 10)));

            services.AddHostedService<OrphanFileCleanupService>();
            services.AddHostedService<AuctionLifecycleBackgroundService>();
            // Singleton as well, so a test or an admin task can run one pass (RunOnceAsync).
            services.AddSingleton<DataRetentionService>();
            services.AddHostedService(sp => sp.GetRequiredService<DataRetentionService>());

            // /health/ready (WebApi): the database must answer; Redis down only degrades (the API works without the cache).
            services.AddHealthChecks()
                    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
                    .AddCheck<RedisHealthCheck>("redis", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, tags: ["ready"]);

            services.AddOptions<SmtpConfiguration>().Bind(configuration.GetSection(SmtpConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddScoped<IEmailService, EmailService>();

            services.AddOptions<EskizConfiguration>().Bind(configuration.GetSection(EskizConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddSingleton<EskizTokenStore>();
            services.AddHttpClient<ISmsSender, EskizSmsSender>((serviceProvider, client) =>
            {
                var eskiz = serviceProvider.GetRequiredService<IOptions<EskizConfiguration>>().Value;
                client.BaseAddress = new Uri(eskiz.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(eskiz.TimeoutSeconds);
            });
            services.AddHostedService<NotificationBackgroundService>();

            // One instance is both the queue the middleware writes to and the hosted service that empties it.
            services.AddSingleton<AuditLogWriterService>();
            services.AddSingleton<IAuditLogQueue>(sp => sp.GetRequiredService<AuditLogWriterService>());
            services.AddHostedService(sp => sp.GetRequiredService<AuditLogWriterService>());

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisOptions = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? "localhost:6379");
                // Keep the app running and retrying in the background when Redis is down.
                redisOptions.AbortOnConnectFail = false;
                // While disconnected, fail cache calls at once instead of queueing each one for 5 s;
                // the cache is optional and callers fall back to the database.
                redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
                redisOptions.AsyncTimeout = 1000;
                redisOptions.SyncTimeout = 1000;
                return ConnectionMultiplexer.Connect(redisOptions);
            });
            services.AddScoped<IRedisService, RedisService>();

            services.AddOptions<JWTConfiguration>().Bind(configuration.GetSection("JWTConfiguration"))
                    .ValidateDataAnnotations().ValidateOnStart();

            var secretWord = configuration["JWTConfiguration:Secret"];
            if (string.IsNullOrWhiteSpace(secretWord) || Encoding.UTF8.GetByteCount(secretWord) < 32)
                throw new InvalidOperationException(
                    "JWTConfiguration:Secret is not configured or shorter than 32 bytes. Set JWTConfiguration__Secret in .env or environment variables.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = configuration["JWTConfiguration:ValidAudience"],
                        ValidIssuer = configuration["JWTConfiguration:ValidIssuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretWord)),
                        // The default 5 minutes would stretch a 30-minute access token by a sixth.
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            // One policy per permission; the list is static, so no service provider is built here.
            services.AddAuthorization(options =>
            {
                foreach (var perm in PermissionCatalog.All)
                {
                    options.AddPolicy(perm, policy => policy.RequireClaim("permission", perm));
                }
            });
            return services;
        }
    }
}

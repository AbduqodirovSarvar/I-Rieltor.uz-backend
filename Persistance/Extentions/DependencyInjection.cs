using Persistance.Services.Maintenance;
using Persistance.Services.Health;
using Application.Authorization;
using Application.Interfaces.Auths;
using Application.Interfaces.Cachings;
using Application.Interfaces.Files;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistance.Data;
using Persistance.Data.Interceptors;
using Persistance.Models.ConfigModels;
using Persistance.Services.Auths;
using Persistance.Services.Cachings;
using Persistance.Services.Files;
using Persistance.Services.Notifications;
using Persistance.Services.Repositories;
using Persistance.Services.Repositories.Abstractions;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Extentions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // No open-generic IGenericRepository<>: it would hand out a repository without the per-entity settings
            // (e.g. IGenericRepository<User> with caching on, putting PasswordHash into Redis).
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IUserRolePermissionRepository, UserRolePermissionRepository>();
            services.AddScoped<IUploadedFileRepository, UploadedFileRepository>();
            services.AddScoped<ISlideRepository, SlideRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPropertyCategoryRepository, PropertyCategoryRepository>();
            services.AddScoped<IPropertyRepository, PropertyRepository>();
            services.AddScoped<IPropertyMediaRepository, PropertyMediaRepository>();
            services.AddScoped<IAuctionRepository, AuctionRepository>();
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<ITokenService, TokenService>();
            // A typo in a number (e.g. TokenHours=0) stops the app at start, not at the first email or sign-in.
            // Missing strings (SMTP host, link base URL) are still reported when used, so one gap breaks only that feature.
            services.AddOptions<EmailConfirmationConfiguration>().Bind(configuration.GetSection(EmailConfirmationConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.AddSingleton<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
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

            services.AddHostedService<OrphanFileCleanupService>();
            // Singleton as well, so a test or an admin task can run one pass (RunOnceAsync).
            services.AddSingleton<DataRetentionService>();
            services.AddHostedService(sp => sp.GetRequiredService<DataRetentionService>());

            // /health/ready (WebApi): the database must answer; Redis down only degrades (the API works without the cache).
            services.AddHealthChecks()
                    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
                    .AddCheck<RedisHealthCheck>("redis", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, tags: ["ready"]);

            services.AddOptions<SmtpConfiguration>().Bind(configuration.GetSection(SmtpConfiguration.SectionName))
                    .ValidateDataAnnotations().ValidateOnStart();
            services.Configure<DefaultAdminConfiguration>(configuration.GetSection(DefaultAdminConfiguration.SectionName));
            services.AddScoped<IEmailService, EmailService>();

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

            var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:PostgreSqlConnection is not configured. Set ConnectionStrings__PostgreSqlConnection in .env or environment variables.");

            services.AddScoped<AuditableEntitySaveChangesInterceptor>();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var interceptor = serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>();
                options.UseNpgsql(connectionString)
                       .AddInterceptors(interceptor);

                // Logs SQL parameter values (password hashes, personal data), so never outside development.
                if (serviceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                    options.EnableSensitiveDataLogging();
            });

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

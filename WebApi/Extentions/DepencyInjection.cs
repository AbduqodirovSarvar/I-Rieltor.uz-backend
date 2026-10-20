using Application.Extentions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using Persistance.Extentions;
using System.Threading.RateLimiting;
using WebApi.Handlers;

namespace WebApi.Extentions
{
    public static class DepencyInjection
    {
        public const string CorsPolicy = "Frontend";
        public const string AuthRateLimitPolicy = "auth";
        public const string SessionRateLimitPolicy = "session";

        public static void AdApiDepencies(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddApplicationServices();
            builder.Services.AddPersistance(builder.Configuration);
            builder.Services.AddSwagger();
            builder.Services.AddFrontendCors(builder.Configuration);
            builder.Services.AddAuthRateLimiting();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.AddUploadFolders();
        }

        /// <summary>
        /// Only the web site and admin panel origins listed in Cors:AllowedOrigins may call the API from a browser.
        /// </summary>
        private static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(o => o.AddPolicy(CorsPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }));

            return services;
        }

        /// <summary>
        /// Limits sign-in, sign-up and password reset attempts per client IP to slow down brute force.
        /// Behind a reverse proxy, forwarded headers must be enabled so RemoteIpAddress is the real client.
        /// </summary>
        private static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                // E-IMZO challenges, token refresh and logout cannot be used to guess credentials
                // (each needs a signature or a 512-bit token), but every active user calls them;
                // a separate, looser limit keeps them from eating the sign-in budget of a shared office IP.
                options.AddPolicy(SessionRateLimitPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            return services;
        }

        private static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "I-Rieltor website's Api",
                    Description = "An ASP.NET Core Web API for I-Rieltor.uz website Api items"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer Authentication",
                    Type = SecuritySchemeType.Http
                });

                // Microsoft.OpenApi 2.x: a requirement points at the scheme through a document-bound reference.
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            return services;
        }

        private static void AddUploadFolders(this WebApplicationBuilder builder)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!Directory.Exists(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
            }

            var uploadedPath = Path.Combine(webRootPath, "uploads");
            if (!Directory.Exists(uploadedPath))
            {
                Directory.CreateDirectory(uploadedPath);
            }

            var deletedPath = Path.Combine(webRootPath, "deleted");
            if (!Directory.Exists(deletedPath))
            {
                Directory.CreateDirectory(deletedPath);
            }

            builder.Environment.WebRootPath = webRootPath;
        }
    }
}

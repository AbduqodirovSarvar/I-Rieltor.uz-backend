using Application.Extentions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using Persistance.Extentions;
using System.Net;
using System.Net.Sockets;
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
            builder.Services.AddReverseProxySupport(builder.Configuration);
            builder.Services.AddAuthRateLimiting();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
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
        /// Behind Nginx every connection comes from the proxy. X-Forwarded-For/-Proto are applied (UseForwardedHeaders,
        /// first in the pipeline) only when they come from a proxy listed in ForwardedHeaders:KnownProxies or
        /// ForwardedHeaders:KnownNetworks (CIDR); from anyone else the header is ignored, otherwise a client could pick
        /// any IP and so its own rate-limit bucket. With nothing listed only loopback is trusted (a proxy on the same host).
        /// RemoteIpAddress is then the real client for the rate limits, RefreshToken.CreatedByIp and E-IMZO's X-Real-IP.
        /// </summary>
        private static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
        {
            var proxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
            var networks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

            // Parsed here, so a typo stops the app at startup instead of silently trusting nobody.
            var knownProxies = proxies.Select(p => IPAddress.TryParse(p, out var ip)
                ? ip : throw new InvalidOperationException($"ForwardedHeaders:KnownProxies: '{p}' is not an IP address.")).ToList();
            var knownNetworks = networks.Select(n => System.Net.IPNetwork.TryParse(n, out var net)
                ? net : throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks: '{n}' is not a CIDR range (e.g. 172.16.0.0/12).")).ToList();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // One proxy hop: only the address Nginx itself saw is taken, earlier entries could be forged by the client.
                options.ForwardLimit = 1;
                if (knownProxies.Count + knownNetworks.Count == 0)
                    return;

                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                knownProxies.ForEach(options.KnownProxies.Add);
                knownNetworks.ForEach(options.KnownIPNetworks.Add);
            });

            return services;
        }

        /// <summary>
        /// Rate-limit bucket of a client: its IPv4 address, or its IPv6 /64. One home or office line gets a whole /64,
        /// so counting single IPv6 addresses would let a client rotate through endless fresh buckets.
        /// </summary>
        internal static string ClientPartitionKey(HttpContext httpContext)
        {
            var address = httpContext.Connection.RemoteIpAddress;
            if (address == null)
                return "unknown";
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                return address.ToString();

            var bytes = address.GetAddressBytes();
            Array.Clear(bytes, 8, 8);
            return $"{new IPAddress(bytes)}/64";
        }

        /// <summary>
        /// Limits sign-in, sign-up and password reset attempts per client (<see cref="ClientPartitionKey"/>) to slow down brute force.
        /// </summary>
        private static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientPartitionKey(httpContext),
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
                        ClientPartitionKey(httpContext),
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
    }
}

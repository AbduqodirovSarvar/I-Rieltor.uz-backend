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
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IUserRolePermissionRepository, UserRolePermissionRepository>();
            services.AddScoped<IUploadedFileRepository, UploadedFileRepository>();
            services.AddScoped<ISlideRepository, SlideRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddHttpClient<IEimzoService, EimzoService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Eimzo:BaseUrl"]!);
            });

            services.AddScoped<IFileService>(provider =>
            {
                var env = provider.GetRequiredService<IWebHostEnvironment>();
                var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(webRootPath))
                    Directory.CreateDirectory(webRootPath);

                return new FileService(webRootPath, configuration["FileStorage:PublicBaseUrl"]);
            });

            services.Configure<SmtpConfiguration>(configuration.GetSection(SmtpConfiguration.SectionName));
            services.Configure<DefaultAdminConfiguration>(configuration.GetSection(DefaultAdminConfiguration.SectionName));
            services.AddScoped<IEmailService, EmailService>();

            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
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
            });

            services.Configure<JWTConfiguration>(configuration.GetSection("JWTConfiguration"));

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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretWord))
                    };
                });



            services.AddAuthorization(options =>
            {
                var sp = services.BuildServiceProvider();
                var permissionService = sp.GetRequiredService<IPermissionService>();
                var allPermissions = permissionService.GetAllPermission().Result;

                foreach (var perm in allPermissions)
                {
                    options.AddPolicy(perm, policy => policy.RequireClaim("permission", perm));
                }
            });
            return services;
        }
    }
}

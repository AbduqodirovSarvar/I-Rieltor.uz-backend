using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Data;
using Persistence.Data.Interceptors;
using Persistence.Models.ConfigModels;
using Persistence.Services.Repositories;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Extensions
{
    /// <summary>The database: context, repositories, unit of work and the dashboard queries. Outside services are in Infrastructure.</summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
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
            services.AddScoped<IAuctionParticipantRepository, AuctionParticipantRepository>();
            services.AddScoped<IBidRepository, BidRepository>();
            services.AddScoped<IAuctionResultRepository, AuctionResultRepository>();
            services.AddScoped<IAuctionAwardRepository, AuctionAwardRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IPrivateDocumentRepository, PrivateDocumentRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<Application.Interfaces.Dashboard.IDashboardReader, Persistence.Services.Dashboard.DashboardReader>();

            services.Configure<DefaultAdminConfiguration>(configuration.GetSection(DefaultAdminConfiguration.SectionName));

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
            return services;
        }
    }
}

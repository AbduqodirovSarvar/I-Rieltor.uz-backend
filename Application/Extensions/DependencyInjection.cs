using Application.Behaviours;
using Application.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Application.Interfaces.Notifications;
using Application.Interfaces.Payments;
using Application.Services.Payments;
using Application.Services.Notifications;
using Application.Interfaces.Auctions;
using Application.Services.Auctions;
using Application.Messaging;
using Mapster;
using MapsterMapper;
using Application.Authorization;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Mappings;
using Application.Services.Auths;
using Application.Services.EntityServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediator(typeof(DependencyInjection).Assembly, typeof(DataAnnotationsValidationBehavior<,>));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserAccessGuard, UserAccessGuard>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IUserRolePermissionService, UserRolePermissionService>();
            services.AddScoped<IUploadedFileService, UploadedFileService>();
            services.AddScoped<ISlideService, SlideService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IPropertyCategoryService, PropertyCategoryService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IAuctionService, AuctionService>();
            services.AddScoped<IAuctionLifecycleService, AuctionLifecycleService>();
            services.AddScoped<IPrivateDocumentService, PrivateDocumentService>();
            services.AddScoped<IParticipationService, ParticipationService>();
            services.AddScoped<IBidService, BidService>();
            services.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection(PaymentSettings.SectionName).Get<PaymentSettings>() ?? new PaymentSettings());
            services.AddScoped<IAuctionSettlement, AuctionSettlement>();
            services.AddScoped<IPaymentProcessor, PaymentProcessor>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IPaymeMerchantService, PaymeMerchantService>();
            services.AddScoped<IClickShopService, ClickShopService>();
            // Replaced by the SignalR implementation in the web host.
            services.TryAddSingleton<IAuctionEvents, NullAuctionEvents>();
            services.AddScoped<IDepositRefundService, DepositRefundService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
            services.AddScoped<NotificationSender>();
            services.AddScoped<AuctionTransitions>();

            // ServiceMapper (not the plain Mapper) gives mapping rules the request's services (MapContext.Current.GetService).
            var mappingConfig = new TypeAdapterConfig();
            mappingConfig.Scan(typeof(MappingConfig).Assembly);
            mappingConfig.Compile();   // a broken rule fails at startup, not on the first request that uses it
            services.AddSingleton(mappingConfig);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}

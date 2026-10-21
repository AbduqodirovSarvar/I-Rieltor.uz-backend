using Application.Behaviours;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extentions
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

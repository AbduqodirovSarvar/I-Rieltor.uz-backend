using Application.Authorization;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.EntityServices.Abstractions;
using Application.Mappings;
using Application.Services.Auths;
using Application.Services.EntityServices;
using Application.Services.EntityServices.Abstractions;
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
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

            services.AddScoped(typeof(IGenericEntityService<,,,>), typeof(GenericEntityService<,,,>));
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserAccessGuard, UserAccessGuard>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IUserRolePermissionService, UserRolePermissionService>();
            services.AddScoped<IUploadedFileService, UploadedFileService>();
            services.AddScoped<ISlideService, SlideService>();
            services.AddScoped<ICompanyService, CompanyService>();

            // Resolvers with dependencies are created by the container.
            services.AddTransient<FileUrlResolver>();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(AssemblyMarker).Assembly);
            });

            return services;
        }
    }
}

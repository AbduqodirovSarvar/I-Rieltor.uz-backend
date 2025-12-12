using Application.Extentions;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Persistance.Extentions;

namespace WebApi.Extentions
{
    public static class DepencyInjection
    {
        public static void AdApiDepencies(this WebApplicationBuilder builder)   
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddApplicationServices();
            builder.Services.AddPersistance(builder.Configuration);
            builder.Services.AddSwagger();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.AddUploadFolders();
        }

        private static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Family Tree website's Api",
                    Description = "An ASP.NET Core Web API for FamilyTree website Api items"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer Authentication",
                    Type = SecuritySchemeType.Http
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });
            });

            services.AddCors(o => o.AddPolicy("AddCors", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            return services;
        }

        private static void AddUploadFolders(this WebApplicationBuilder builder)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!Directory.Exists(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
            }

            var uploadedPath = Path.Combine(webRootPath, "uploaded");
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

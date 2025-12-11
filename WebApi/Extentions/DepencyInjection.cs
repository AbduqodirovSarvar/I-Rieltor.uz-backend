using Application.Extentions;
using Persistance.Extentions;

namespace WebApi.Extentions
{
    public static class DepencyInjection
    {
        public static void AdApiDepencies(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddApplicationServices();
            builder.Services.AddPersistance(builder.Configuration);
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
        }
    }
}

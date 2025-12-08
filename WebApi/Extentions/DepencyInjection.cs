namespace WebApi.Extentions
{
    public static class DepencyInjection
    {
        public static void AdApiDepencies(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddHttpContextAccessor();
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
        }
    }
}

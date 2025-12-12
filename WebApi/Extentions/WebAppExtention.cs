using Application.Interfaces.Auths;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Persistance.Data.Seeds;

namespace WebApi.Extentions
{
    public static class WebAppExtention
    {
        public static async Task AddWebAppExtention(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "I-Rieltor.uz API V1");
                c.RoutePrefix = string.Empty;
            });
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("AddCors");
            app.MapControllers();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            await app.UpdateMigration();
            await app.SeedAsync();
        }

        private static async Task UpdateMigration(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        private static async Task SeedAsync(this WebApplication app)
        {
            var scope = app.Services;
            await DefaultDataSeeder.SeedAsync(scope);
        }
    }
}

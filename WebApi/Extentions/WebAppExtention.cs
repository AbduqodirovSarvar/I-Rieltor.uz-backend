using Application.Interfaces.Auths;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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
            app.UseUploadedFiles();
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

        /// <summary>
        /// Serves files from wwwroot/uploads at /uploads as static files, without hitting controllers or the database.
        /// Only this folder is exposed (wwwroot/deleted stays private), and only whitelisted file types are served.
        /// </summary>
        private static void UseUploadedFiles(this WebApplication app)
        {
            var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var contentTypes = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".png"] = "image/png",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".gif"] = "image/gif",
                [".pdf"] = "application/pdf",
                [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".mp4"] = "video/mp4",
                [".mp3"] = "audio/mpeg",
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads",
                ContentTypeProvider = contentTypes,
                ServeUnknownFileTypes = false,
                OnPrepareResponse = ctx =>
                {
                    var headers = ctx.Context.Response.Headers;
                    headers.XContentTypeOptions = "nosniff";
                    // File names are unique GUIDs and a replaced file gets a new name, so the content never changes.
                    headers.CacheControl = "public, max-age=31536000, immutable";
                }
            });
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

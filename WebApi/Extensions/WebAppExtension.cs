using WebApi.Hubs;
using WebApi.Middlewares;
using Serilog.Events;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Application.Interfaces.Auths;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Persistence.Data;
using Persistence.Data.Seeds;
using Infrastructure.Services.Files;

namespace WebApi.Extensions
{
    public static class WebAppExtension
    {
        public static async Task AddWebAppExtension(this WebApplication app)
        {
            // First: everything after it (rate limits, logging, HTTPS redirect, E-IMZO X-Real-IP) must see the real client.
            app.UseForwardedHeaders();
            app.UseMiddleware<CorrelationIdMiddleware>();
            // One line per request (method, path, status, time); before the exception handler, so a 500 is logged as such.
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (context, _, exception) =>
                    context.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                    : exception != null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                    : LogEventLevel.Information;
            });
            // Outside the exception handler: it records the status the client really got.
            app.UseMiddleware<AuditLogMiddleware>();
            app.UseExceptionHandler();

            // API documentation is exposed only on developer machines.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "I-Rieltor.uz API V1");
                    c.RoutePrefix = string.Empty;
                });
            }

            app.UseCors(WebApiDependencyInjection.CorsPolicy);
            app.UseUploadedFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();
            app.MapHub<AuctionHub>("/hubs/auction");
            // live: the process answers (no dependencies); ready: the database too (used by the Docker healthcheck).
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

            // Database:MigrateOnStartup (default true): with several instances, or migrations applied by a CI step /
            // migration bundle, set it to false so a start never changes the schema.
            if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
                await app.UpdateMigration();
            else
                app.Logger.LogInformation("Database:MigrateOnStartup is false; pending migrations are not applied");
            await app.SeedAsync();
        }

        /// <summary>
        /// Serves files from &lt;FileStorage:RootPath&gt;/uploads at /uploads as static files, without hitting controllers or the database.
        /// Only this folder is exposed (deleted/ next to it stays private), and only whitelisted file types are served.
        /// </summary>
        private static void UseUploadedFiles(this WebApplication app)
        {
            var rootPath = FileStorageLocation.ResolveRootPath(app.Configuration, app.Environment.ContentRootPath);
            var uploadsPath = FileStorageLocation.UploadsPath(rootPath);
            // Fails at startup, not on the first upload, when the folder is not writable (e.g. a volume owned by root).
            Directory.CreateDirectory(uploadsPath);
            Directory.CreateDirectory(Path.Combine(rootPath, "deleted"));
            app.Logger.LogInformation("Uploaded files are stored in {RootPath}", rootPath);

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

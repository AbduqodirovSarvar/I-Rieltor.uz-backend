using WebApi.Extensions;

// Docker HEALTHCHECK: the runtime image has no curl, so the app checks itself (see HealthProbe).
if (args.Length > 0 && args[0] == HealthProbe.Argument)
    return await HealthProbe.RunAsync();

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDependencies();

var app = builder.Build();

await app.AddWebAppExtension();

app.Run();
return 0;

/// <summary>
/// Public so the integration tests can start the API with WebApplicationFactory&lt;Program&gt;.
/// </summary>
public partial class Program
{
}

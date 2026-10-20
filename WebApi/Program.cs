using WebApi.Extentions;

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.AdApiDepencies();

var app = builder.Build();

await app.AddWebAppExtention();

app.Run();

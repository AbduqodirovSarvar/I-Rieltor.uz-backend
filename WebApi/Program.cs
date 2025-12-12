using WebApi.Extentions;

var builder = WebApplication.CreateBuilder(args);

builder.AdApiDepencies();

var app = builder.Build();

await app.AddWebAppExtention();

app.Run();

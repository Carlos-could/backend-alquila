using Backend.Alquila.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

DotEnvLoader.LoadIfExists(Path.Combine(builder.Environment.ContentRootPath, ".env"));
EnvironmentValidator.ValidateOrThrow();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new { service = "backend-alquila", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

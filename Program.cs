using System.Security.Claims;
using Backend.Alquila.Features.Auth;
using Backend.Alquila.Features.Properties;
using Backend.Alquila.Infrastructure.Configuration;
using Backend.Alquila.Infrastructure.Persistence.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

DotEnvLoader.LoadIfExists(Path.Combine(builder.Environment.ContentRootPath, ".env"));

var assignedPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(assignedPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{assignedPort}");
}
else
{
    var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        builder.WebHost.UseUrls(configuredUrls);
    }
}

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "uploads"));

if (MigrationRunner.IsMigrationCommand(args))
{
    var result = await MigrationRunner.RunAsync(args, builder.Environment.ContentRootPath);
    Environment.ExitCode = result;
    return;
}

EnvironmentValidator.ValidateOrThrow();

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")!.TrimEnd('/');
var supabaseIssuer = $"{supabaseUrl}/auth/v1";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = supabaseIssuer;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = supabaseIssuer,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.InquilinoOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => RoleClaimResolver.IsInAnyRole(context.User, UserRoles.Inquilino)));

    options.AddPolicy(AuthorizationPolicies.PropietarioOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => RoleClaimResolver.IsInAnyRole(context.User, UserRoles.Propietario)));

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => RoleClaimResolver.IsInAnyRole(context.User, UserRoles.Admin)));
});

builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPropertiesRepository, NpgsqlPropertiesRepository>();
builder.Services.AddSingleton<IPropertyImageStorage, LocalPropertyImageStorage>();
builder.Services.AddCors(options =>
{
    var allowedOriginsRaw = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
    var allowedOrigins = string.IsNullOrWhiteSpace(allowedOriginsRaw)
        ? new[] { "http://localhost:3000" }
        : allowedOriginsRaw
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("FrontendCors");
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "backend-alquila", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/auth/me", (ClaimsPrincipal user) =>
{
    var role = RoleClaimResolver.Resolve(user);
    return Results.Ok(new
    {
        userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
        role
    });
}).RequireAuthorization();

app.MapGet("/admin", () => Results.Ok(new { area = "admin", access = "granted" }))
    .RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapGet("/propietario", () => Results.Ok(new { area = "propietario", access = "granted" }))
    .RequireAuthorization(AuthorizationPolicies.PropietarioOnly);

app.MapGet("/inquilino", () => Results.Ok(new { area = "inquilino", access = "granted" }))
    .RequireAuthorization(AuthorizationPolicies.InquilinoOnly);

app.MapPropertyEndpoints();
app.MapGroup("/api/v1").MapPropertyEndpoints();

app.Run();

public partial class Program;

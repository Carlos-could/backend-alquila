using System.Security.Claims;
using Backend.Alquila.Features.Auth;
using Backend.Alquila.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

DotEnvLoader.LoadIfExists(Path.Combine(builder.Environment.ContentRootPath, ".env"));
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

var app = builder.Build();

app.UseExceptionHandler();
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

app.Run();

public partial class Program;

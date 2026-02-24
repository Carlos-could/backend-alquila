using System.Security.Claims;
using Backend.Alquila.Features.Auth;

namespace Backend.Alquila.Features.Properties;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/properties").RequireAuthorization();

        group.MapPost("/", CreatePropertyAsync);
        group.MapPatch("/{id:guid}", PatchPropertyAsync);

        return app;
    }

    private static async Task<IResult> CreatePropertyAsync(
        ClaimsPrincipal user,
        PropertyUpsertRequest request,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Propietario, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var errors = PropertyValidator.ValidateForCreate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryGetAuthUserId(user, out var authUserId))
        {
            return Results.Unauthorized();
        }

        var ownerUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
        if (!ownerUserId.HasValue)
        {
            return Results.Problem(
                title: "Missing user profile",
                detail: "Authenticated user does not have an internal profile in users table.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var created = await repository.CreateAsync(
            ownerUserId.Value,
            PropertyMappings.NormalizeForCreate(request),
            cancellationToken);

        return Results.Created($"/properties/{created.Id}", PropertyMappings.ToResponse(created));
    }

    private static async Task<IResult> PatchPropertyAsync(
        Guid id,
        ClaimsPrincipal user,
        PropertyPatchRequest request,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Propietario, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var errors = PropertyValidator.ValidateForPatch(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var normalizedPatch = PropertyMappings.NormalizeForPatch(request);
        if (!normalizedPatch.HasAnyField)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "At least one field is required for patch." }
            });
        }

        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return Results.NotFound();
        }

        var isAdmin = RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin);
        if (!isAdmin)
        {
            if (!TryGetAuthUserId(user, out var authUserId))
            {
                return Results.Unauthorized();
            }

            var ownerUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
            if (!ownerUserId.HasValue || ownerUserId.Value != current.OwnerUserId)
            {
                return Results.Forbid();
            }
        }

        var updated = await repository.UpdateAsync(id, normalizedPatch, cancellationToken);
        if (updated is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PropertyMappings.ToResponse(updated));
    }

    private static bool TryGetAuthUserId(ClaimsPrincipal user, out Guid authUserId)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out authUserId);
    }
}

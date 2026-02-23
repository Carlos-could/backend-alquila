using System.Security.Claims;
using System.Text.Json;

namespace Backend.Alquila.Features.Auth;

public static class RoleClaimResolver
{
    private static readonly string[] DirectRoleClaimTypes =
    {
        ClaimTypes.Role,
        "role",
        "user_role",
        "app_role"
    };

    private static readonly string[] MetadataClaimTypes =
    {
        "app_metadata",
        "user_metadata"
    };

    public static string? Resolve(ClaimsPrincipal user)
    {
        foreach (var claimType in DirectRoleClaimTypes)
        {
            var claimValue = user.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(claimValue))
            {
                return claimValue.Trim().ToLowerInvariant();
            }
        }

        foreach (var claimType in MetadataClaimTypes)
        {
            var claimValue = user.FindFirstValue(claimType);
            var roleFromMetadata = ExtractRoleFromJson(claimValue);
            if (!string.IsNullOrWhiteSpace(roleFromMetadata))
            {
                return roleFromMetadata;
            }
        }

        return null;
    }

    public static bool IsInAnyRole(ClaimsPrincipal user, params string[] allowedRoles)
    {
        var resolvedRole = Resolve(user);
        if (string.IsNullOrWhiteSpace(resolvedRole))
        {
            return false;
        }

        return allowedRoles.Any(role => string.Equals(role, resolvedRole, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractRoleFromJson(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            if (!document.RootElement.TryGetProperty("role", out var roleElement))
            {
                return null;
            }

            var role = roleElement.GetString();
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return role.Trim().ToLowerInvariant();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

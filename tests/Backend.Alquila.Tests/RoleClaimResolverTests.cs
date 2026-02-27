using System.Security.Claims;
using Backend.Alquila.Features.Auth;

namespace Backend.Alquila.Tests;

public sealed class RoleClaimResolverTests
{
    [Fact]
    public void Resolve_WithGenericDirectRole_UsesMetadataRole()
    {
        var user = BuildUser(new[]
        {
            new Claim("role", "authenticated"),
            new Claim("app_metadata", "{\"role\":\"propietario\"}")
        });

        var role = RoleClaimResolver.Resolve(user);

        Assert.Equal("propietario", role);
    }

    [Fact]
    public void Resolve_WithSpecificDirectRole_PrefersDirectRole()
    {
        var user = BuildUser(new[]
        {
            new Claim("role", "admin"),
            new Claim("app_metadata", "{\"role\":\"propietario\"}")
        });

        var role = RoleClaimResolver.Resolve(user);

        Assert.Equal("admin", role);
    }

    private static ClaimsPrincipal BuildUser(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        return new ClaimsPrincipal(identity);
    }
}

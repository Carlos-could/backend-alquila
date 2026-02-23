namespace Backend.Alquila.Features.Auth;

public static class AuthorizationPolicies
{
    public const string InquilinoOnly = nameof(InquilinoOnly);
    public const string PropietarioOnly = nameof(PropietarioOnly);
    public const string AdminOnly = nameof(AdminOnly);
}

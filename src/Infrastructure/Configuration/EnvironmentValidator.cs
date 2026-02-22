namespace Backend.Alquila.Infrastructure.Configuration;

public static class EnvironmentValidator
{
    private static readonly string[] RequiredKeys =
    {
        "SUPABASE_URL",
        "SUPABASE_ANON_KEY"
    };

    public static void ValidateOrThrow()
    {
        var missingKeys = RequiredKeys
            .Where(IsMissing)
            .ToList();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProduction = string.Equals(
            environment,
            "Production",
            StringComparison.OrdinalIgnoreCase);

        if (isProduction && IsMissing("SUPABASE_SERVICE_ROLE_KEY"))
        {
            missingKeys.Add("SUPABASE_SERVICE_ROLE_KEY");
        }

        if (missingKeys.Count == 0)
        {
            return;
        }

        var joinedKeys = string.Join(", ", missingKeys);
        throw new InvalidOperationException(
            $"Missing required environment variables: {joinedKeys}. " +
            "Set them in the process environment or in a local .env file based on .env.example.");
    }

    private static bool IsMissing(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.StartsWith("__SET_ME", StringComparison.OrdinalIgnoreCase);
    }
}

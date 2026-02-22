namespace Backend.Alquila.Infrastructure.Configuration;

public static class EnvironmentValidator
{
    private static readonly string[] RequiredKeys =
    {
        "SUPABASE_URL",
        "SUPABASE_ANON_KEY",
        "SUPABASE_SERVICE_ROLE_KEY"
    };

    public static void ValidateOrThrow()
    {
        var missingKeys = RequiredKeys
            .Where(IsMissing)
            .ToArray();

        if (missingKeys.Length == 0)
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

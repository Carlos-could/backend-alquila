using Npgsql;

namespace Backend.Alquila.Infrastructure.Persistence.Database;

public static class DatabaseUrlResolver
{
    public static bool TryResolve(out string databaseUrl, out string errorMessage)
    {
        var rawValue = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawValue) || rawValue.StartsWith("__SET_ME", StringComparison.OrdinalIgnoreCase))
        {
            databaseUrl = string.Empty;
            errorMessage = "DATABASE_URL is required.";
            return false;
        }

        if (rawValue.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            rawValue.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return TryResolveFromUri(rawValue, out databaseUrl, out errorMessage);
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(rawValue);
            databaseUrl = builder.ConnectionString;
            errorMessage = string.Empty;
            return true;
        }
        catch (ArgumentException)
        {
            databaseUrl = string.Empty;
            errorMessage = "DATABASE_URL format is invalid. Use key-value format or postgres:// URI.";
            return false;
        }
    }

    private static bool TryResolveFromUri(string rawValue, out string databaseUrl, out string errorMessage)
    {
        if (!Uri.TryCreate(rawValue, UriKind.Absolute, out var uri))
        {
            databaseUrl = string.Empty;
            errorMessage = "DATABASE_URL URI is invalid.";
            return false;
        }

        var userInfoParts = uri.UserInfo.Split(':', 2);
        if (userInfoParts.Length != 2)
        {
            databaseUrl = string.Empty;
            errorMessage = "DATABASE_URL must include username and password.";
            return false;
        }

        var username = Uri.UnescapeDataString(userInfoParts[0]);
        var password = Uri.UnescapeDataString(userInfoParts[1]);
        var database = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(database))
        {
            database = "postgres";
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = username,
            Password = password,
            Database = database
        };

        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrWhiteSpace(query))
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]).Trim();
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]).Trim() : string.Empty;

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<SslMode>(value, true, out var sslMode))
                {
                    builder.SslMode = sslMode;
                }
            }
        }

        databaseUrl = builder.ConnectionString;
        errorMessage = string.Empty;
        return true;
    }
}

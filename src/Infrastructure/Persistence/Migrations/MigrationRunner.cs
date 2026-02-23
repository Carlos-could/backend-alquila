using System.Data;
using Npgsql;

namespace Backend.Alquila.Infrastructure.Persistence.Migrations;

public static class MigrationRunner
{
    public static async Task<int> RunAsync(string[] args, string contentRootPath, CancellationToken cancellationToken = default)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Missing migration command. Use: migrate up | migrate down");
            return 1;
        }

        var command = args[1];

        if (!TryResolveDatabaseUrl(out var databaseUrl, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            return 1;
        }

        var migrationDirectory = Path.Combine(contentRootPath, "database", "migrations");
        if (!Directory.Exists(migrationDirectory))
        {
            Console.Error.WriteLine($"Migration directory was not found: {migrationDirectory}");
            return 1;
        }

        await using var connection = new NpgsqlConnection(databaseUrl);
        await connection.OpenAsync(cancellationToken);
        await EnsureHistoryTableExistsAsync(connection, cancellationToken);

        return command switch
        {
            "up" => await ApplyPendingMigrationsAsync(connection, migrationDirectory, cancellationToken),
            "down" => await RollbackLastMigrationAsync(connection, migrationDirectory, cancellationToken),
            _ => WriteUnsupportedCommand(command)
        };
    }

    public static bool IsMigrationCommand(string[] args) =>
        args.Length > 0 && string.Equals(args[0], "migrate", StringComparison.OrdinalIgnoreCase);

    private static int WriteUnsupportedCommand(string command)
    {
        Console.Error.WriteLine($"Unsupported migration command '{command}'. Use: migrate up | migrate down");
        return 1;
    }

    private static async Task<int> ApplyPendingMigrationsAsync(
        NpgsqlConnection connection,
        string migrationDirectory,
        CancellationToken cancellationToken)
    {
        var scripts = Directory
            .GetFiles(migrationDirectory, "*.up.sql")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (scripts.Count == 0)
        {
            Console.WriteLine("No up migration scripts found.");
            return 0;
        }

        var appliedVersions = await GetAppliedVersionsAsync(connection, cancellationToken);

        foreach (var scriptPath in scripts)
        {
            var version = Path.GetFileName(scriptPath).Replace(".up.sql", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (appliedVersions.Contains(version))
            {
                continue;
            }

            var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            await ExecuteMigrationAsync(connection, version, sql, isUp: true, cancellationToken);
            Console.WriteLine($"Applied migration: {version}");
        }

        Console.WriteLine("Migration up finished.");
        return 0;
    }

    private static async Task<int> RollbackLastMigrationAsync(
        NpgsqlConnection connection,
        string migrationDirectory,
        CancellationToken cancellationToken)
    {
        const string latestMigrationSql = """
            select version
            from public.schema_migrations
            order by version desc
            limit 1;
            """;

        await using var latestCommand = new NpgsqlCommand(latestMigrationSql, connection);
        var latestVersionObj = await latestCommand.ExecuteScalarAsync(cancellationToken);
        var latestVersion = latestVersionObj as string;

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Console.WriteLine("No applied migrations to roll back.");
            return 0;
        }

        var downScriptPath = Path.Combine(migrationDirectory, $"{latestVersion}.down.sql");
        if (!File.Exists(downScriptPath))
        {
            Console.Error.WriteLine($"Down migration not found for version '{latestVersion}': {downScriptPath}");
            return 1;
        }

        var sql = await File.ReadAllTextAsync(downScriptPath, cancellationToken);
        await ExecuteMigrationAsync(connection, latestVersion, sql, isUp: false, cancellationToken);
        Console.WriteLine($"Rolled back migration: {latestVersion}");
        return 0;
    }

    private static async Task ExecuteMigrationAsync(
        NpgsqlConnection connection,
        string version,
        string migrationSql,
        bool isUp,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        await using (var migrationCommand = new NpgsqlCommand(migrationSql, connection, transaction))
        {
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (isUp)
        {
            const string insertHistorySql = """
                insert into public.schema_migrations (version, applied_at)
                values (@version, now());
                """;

            await using var historyCommand = new NpgsqlCommand(insertHistorySql, connection, transaction);
            historyCommand.Parameters.AddWithValue("version", version);
            await historyCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            const string deleteHistorySql = """
                delete from public.schema_migrations
                where version = @version;
                """;

            await using var historyCommand = new NpgsqlCommand(deleteHistorySql, connection, transaction);
            historyCommand.Parameters.AddWithValue("version", version);
            await historyCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> GetAppliedVersionsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select version
            from public.schema_migrations;
            """;

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static async Task EnsureHistoryTableExistsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists public.schema_migrations (
                version text primary key,
                applied_at timestamptz not null default now()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool TryResolveDatabaseUrl(out string databaseUrl, out string errorMessage)
    {
        var rawValue = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(rawValue) && !rawValue.StartsWith("__SET_ME", StringComparison.OrdinalIgnoreCase))
        {
            if (TryNormalizeConnectionString(rawValue, out databaseUrl))
            {
                errorMessage = string.Empty;
                return true;
            }

            databaseUrl = string.Empty;
            errorMessage = "DATABASE_URL format is invalid. Use either key-value format or a postgres:// / postgresql:// URL.";
            return false;
        }

        databaseUrl = string.Empty;
        errorMessage = "DATABASE_URL is required for migrations. Set it in environment or .env before running 'migrate up/down'.";
        return false;
    }

    private static bool TryNormalizeConnectionString(string rawValue, out string normalized)
    {
        normalized = string.Empty;

        if (rawValue.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            rawValue.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(rawValue, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var userInfoParts = uri.UserInfo.Split(':', 2);
            if (userInfoParts.Length != 2)
            {
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

            normalized = builder.ConnectionString;
            return true;
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(rawValue);
            normalized = builder.ConnectionString;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

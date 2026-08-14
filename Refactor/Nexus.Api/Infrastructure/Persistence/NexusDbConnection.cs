namespace Refactor.Nexus.Api.Infrastructure.Persistence;

/// <summary>
/// Resolves the shared Nexus application database connection string
/// (Accounts, Journal, and future domains on the same Postgres).
/// </summary>
public static class NexusDbConnection
{
    public const string ConnectionStringName = "AccountsDb";
    public const string EnvironmentVariableName = "NEXUS_ACCOUNTS_DB_CONNECTION";

    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return Normalize(fromEnvironment.Trim());

        var fromConfiguration = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
            return Normalize(fromConfiguration.Trim());

        throw new InvalidOperationException(
            $"{ConnectionStringName} was not configured. Set {EnvironmentVariableName} or ConnectionStrings:{ConnectionStringName}.");
    }

    /// <summary>
    /// Npgsql keyword form only. Converts <c>postgres(ql)://</c> URIs used by Neon/etc.
    /// </summary>
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');
        var sslMode = "Prefer";

        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length != 2)
                    continue;

                if (pair[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    sslMode = MapSslMode(Uri.UnescapeDataString(pair[1]));
            }
        }

        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode}";
    }

    private static string MapSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => "Disable",
        "allow" => "Allow",
        "prefer" => "Prefer",
        "require" => "Require",
        "verify-ca" => "VerifyCA",
        "verify-full" => "VerifyFull",
        _ => "Prefer"
    };
}

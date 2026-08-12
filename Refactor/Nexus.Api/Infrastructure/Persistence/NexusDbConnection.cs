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
            return fromEnvironment.Trim();

        var fromConfiguration = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
            return fromConfiguration.Trim();

        throw new InvalidOperationException(
            $"{ConnectionStringName} was not configured. Set {EnvironmentVariableName} or ConnectionStrings:{ConnectionStringName}.");
    }
}

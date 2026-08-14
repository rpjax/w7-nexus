namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;

public sealed class SeedAdministratorSettings
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool CreationTokenConfigured { get; init; }

    public static SeedAdministratorSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var creationToken = Environment.GetEnvironmentVariable("NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN")
            ?? configuration["Accounts:AdministratorCreationToken"];

        return new SeedAdministratorSettings
        {
            Username = FirstNonEmpty(
                Environment.GetEnvironmentVariable("NEXUS_SEED_ADMIN_USERNAME"),
                configuration["Accounts:SeedAdmin:Username"]),
            Password = Environment.GetEnvironmentVariable("NEXUS_SEED_ADMIN_PASSWORD")
                ?? configuration["Accounts:SeedAdmin:Password"],
            CreationTokenConfigured = !string.IsNullOrWhiteSpace(creationToken)
        };
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

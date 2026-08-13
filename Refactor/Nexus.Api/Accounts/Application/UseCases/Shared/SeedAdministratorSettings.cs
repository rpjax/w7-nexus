namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;

public sealed class SeedAdministratorSettings
{
    public string? Handle { get; init; }
    public string? Password { get; init; }
    public bool CreationTokenConfigured { get; init; }

    public static SeedAdministratorSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var creationToken = Environment.GetEnvironmentVariable("NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN")
            ?? configuration["Accounts:AdministratorCreationToken"];

        return new SeedAdministratorSettings
        {
            Handle = Environment.GetEnvironmentVariable("NEXUS_SEED_ADMIN_HANDLE")
                ?? configuration["Accounts:SeedAdmin:Handle"],
            Password = Environment.GetEnvironmentVariable("NEXUS_SEED_ADMIN_PASSWORD")
                ?? configuration["Accounts:SeedAdmin:Password"],
            CreationTokenConfigured = !string.IsNullOrWhiteSpace(creationToken)
        };
    }
}

using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Security;

public sealed class ConfigurationAdministratorCreationTokenService : IAdministratorCreationTokenService
{
    private readonly IConfiguration _configuration;

    public ConfigurationAdministratorCreationTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsAuthorizedAsync(string? providedToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expectedToken = Environment.GetEnvironmentVariable("NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN")
            ?? _configuration["Accounts:AdministratorCreationToken"];

        if (string.IsNullOrWhiteSpace(expectedToken) || string.IsNullOrWhiteSpace(providedToken))
            return Task.FromResult(false);

        return Task.FromResult(string.Equals(providedToken.Trim(), expectedToken.Trim(), StringComparison.Ordinal));
    }
}

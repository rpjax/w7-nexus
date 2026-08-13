using Microsoft.Extensions.Logging;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;

public sealed class SeedAdministrator
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountReadRepository _accountReadRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJournalWriter _journal;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedAdministrator> _logger;

    public SeedAdministrator(
        IAccountRepository accountRepository,
        IAccountReadRepository accountReadRepository,
        IPasswordHasher passwordHasher,
        IJournalWriter journal,
        IConfiguration configuration,
        ILogger<SeedAdministrator> logger)
    {
        _accountRepository = accountRepository;
        _accountReadRepository = accountReadRepository;
        _passwordHasher = passwordHasher;
        _journal = journal;
        _configuration = configuration;
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(SeedAdministratorSettings.From(_configuration), cancellationToken);

    public async Task ExecuteAsync(
        SeedAdministratorSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var administratorCount = await _accountReadRepository.CountByRoleAsync(
            Roles.Administrator,
            cancellationToken);

        var decision = SeedAdministratorPolicy.Evaluate(
            administratorCount,
            settings.Handle,
            settings.Password,
            settings.CreationTokenConfigured);

        switch (decision)
        {
            case SeedAdministratorDecision.SkipAlreadyHasAdministrator:
                _logger.LogInformation("Seed Admin skipped: at least one Administrator already exists.");
                return;
            case SeedAdministratorDecision.SkipNotConfigured:
                _logger.LogInformation(
                    "Seed Admin skipped: NEXUS_SEED_ADMIN_HANDLE / NEXUS_SEED_ADMIN_PASSWORD (or Accounts:SeedAdmin) not fully set.");
                return;
            case SeedAdministratorDecision.SkipMissingCreationTokenGuard:
                _logger.LogWarning(
                    "Seed Admin skipped: handle/password are set but NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN (or Accounts:AdministratorCreationToken) is empty.");
                return;
            case SeedAdministratorDecision.Seed:
                break;
            default:
                return;
        }

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            settings.Handle!,
            settings.Password!,
            _accountReadRepository,
            cancellationToken);

        if (errors.Count > 0)
        {
            var detail = string.Join("; ", errors.Select(error => $"{error.Code}: {error.Message}"));
            throw new InvalidOperationException($"Seed Admin failed validation: {detail}");
        }

        var passwordHash = await _passwordHasher.HashAsync(settings.Password!, cancellationToken);
        var account = Account.Create(settings.Handle!.Trim(), passwordHash, [Roles.Administrator]);
        account = await _accountRepository.CreateAsync(account, cancellationToken);
        _journal.RecordCreated(account);

        _logger.LogInformation("Seed Admin created handle '{Handle}'.", account.Username);
    }
}

public static class SeedAdministratorExtensions
{
    public static async Task SeedAdministratorIfNeededAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<SeedAdministrator>();
        await seeder.ExecuteAsync();
    }
}

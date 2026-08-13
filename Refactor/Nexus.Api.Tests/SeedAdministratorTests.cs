using Microsoft.Extensions.Logging.Abstractions;
using Refactor.Nexus.Api.Accounts.Application.Journal;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class SeedAdministratorTests
{
    [Fact]
    public void Policy_is_idempotent_when_an_administrator_already_exists()
    {
        var decision = SeedAdministratorPolicy.Evaluate(1, "seed.admin", "password1", creationTokenConfigured: true);
        Assert.Equal(SeedAdministratorDecision.SkipAlreadyHasAdministrator, decision);
    }

    [Fact]
    public void Policy_seeds_when_zero_admins_and_guard_is_present()
    {
        var decision = SeedAdministratorPolicy.Evaluate(0, "seed.admin", "password1", creationTokenConfigured: true);
        Assert.Equal(SeedAdministratorDecision.Seed, decision);
    }

    [Fact]
    public void Policy_skips_when_not_configured()
    {
        var decision = SeedAdministratorPolicy.Evaluate(0, null, null, creationTokenConfigured: true);
        Assert.Equal(SeedAdministratorDecision.SkipNotConfigured, decision);
    }

    [Fact]
    public void Policy_requires_existing_creation_token_as_guard()
    {
        var decision = SeedAdministratorPolicy.Evaluate(0, "seed.admin", "password1", creationTokenConfigured: false);
        Assert.Equal(SeedAdministratorDecision.SkipMissingCreationTokenGuard, decision);
    }

    [Fact]
    public async Task Execute_creates_admin_once_then_skips()
    {
        var accounts = new InMemoryAccountRepository();
        var journal = new RecordingJournalWriter();
        var seeder = new SeedAdministrator(
            accounts,
            accounts,
            new FakePasswordHasher(),
            journal,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            NullLogger<SeedAdministrator>.Instance);

        var settings = new SeedAdministratorSettings
        {
            Handle = "seed.admin",
            Password = "password1",
            CreationTokenConfigured = true
        };

        await seeder.ExecuteAsync(settings);
        await seeder.ExecuteAsync(settings);

        Assert.Equal(1, await accounts.CountByRoleAsync(Roles.Administrator));
        Assert.NotNull(await accounts.FindByUsernameAsync("seed.admin"));
        Assert.Single(journal.Facts.OfType<AccountCreated>());
    }

    [Fact]
    public async Task Execute_does_not_recreate_when_admin_already_exists()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.CreateAsync(Account.Create("existing.admin", "hash", [Roles.Administrator]));
        var journal = new RecordingJournalWriter();
        var seeder = new SeedAdministrator(
            accounts,
            accounts,
            new FakePasswordHasher(),
            journal,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            NullLogger<SeedAdministrator>.Instance);

        await seeder.ExecuteAsync(new SeedAdministratorSettings
        {
            Handle = "another.admin",
            Password = "password1",
            CreationTokenConfigured = true
        });

        Assert.Equal(1, await accounts.CountByRoleAsync(Roles.Administrator));
        Assert.Null(await accounts.FindByUsernameAsync("another.admin"));
        Assert.Empty(journal.Facts);
    }
}

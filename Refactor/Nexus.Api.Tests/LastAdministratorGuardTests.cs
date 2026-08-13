using Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class LastAdministratorGuardTests
{
    [Fact]
    public async Task Last_administrator_cannot_be_disabled()
    {
        var accounts = new InMemoryAccountRepository();
        var admin = await accounts.CreateAsync(Account.Create("only.admin", "hash", [Roles.Administrator]));

        var error = await AccountAdministratorGuards.EnsureNotLastAdministratorAsync(
            admin,
            roleBeingRemoved: null,
            accounts,
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(AccountErrorCodes.CannotRemoveLastAdministrator, error.Code);
    }

    [Fact]
    public async Task Last_administrator_cannot_have_administrator_role_revoked()
    {
        var accounts = new InMemoryAccountRepository();
        var admin = await accounts.CreateAsync(Account.Create("only.admin", "hash", [Roles.Administrator]));

        var error = await AccountAdministratorGuards.EnsureNotLastAdministratorAsync(
            admin,
            Roles.Administrator,
            accounts,
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(AccountErrorCodes.CannotRemoveLastAdministrator, error.Code);
    }

    [Fact]
    public async Task Second_administrator_can_be_disabled()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.CreateAsync(Account.Create("admin.one", "hash", [Roles.Administrator]));
        var other = await accounts.CreateAsync(Account.Create("admin.two", "hash", [Roles.Administrator]));

        var error = await AccountAdministratorGuards.EnsureNotLastAdministratorAsync(
            other,
            roleBeingRemoved: null,
            accounts,
            CancellationToken.None);

        Assert.Null(error);
    }
}

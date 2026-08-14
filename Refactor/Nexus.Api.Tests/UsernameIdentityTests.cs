using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class UsernameIdentityTests
{
    [Fact]
    public async Task Create_rejects_username_already_in_use()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.CreateAsync(Account.Create("alice", "hash"));

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            "Alice",
            "password1",
            accounts,
            CancellationToken.None);

        Assert.Contains(errors, error => error.Code == AccountErrorCodes.UsernameAlreadyTaken);
    }

    [Fact]
    public async Task Create_rejects_retired_username()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.RetireUsernameAsync("alice", AccountId.New());

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            "alice",
            "password1",
            accounts,
            CancellationToken.None);

        Assert.Contains(errors, error => error.Code == AccountErrorCodes.UsernameRetired);
        Assert.True(await accounts.IsUsernameTakenAsync("alice"));
    }

    [Fact]
    public async Task Rename_reserves_previous_username_and_blocks_reuse()
    {
        var accounts = new InMemoryAccountRepository();
        var account = await accounts.CreateAsync(Account.Create("old.user", "hash"));
        account.ChangeUsername("new.user");
        await accounts.UpdateChangingUsernameAsync(account, "old.user");

        Assert.Null(await accounts.FindByUsernameAsync("old.user"));
        Assert.NotNull(await accounts.FindByUsernameAsync("new.user"));
        Assert.True(await accounts.IsUsernameRetiredAsync("old.user"));

        var createErrors = await AccountRegistrationPolicy.ValidateAsync(
            "old.user",
            "password1",
            accounts,
            CancellationToken.None);
        var renameErrors = await AccountRegistrationPolicy.ValidateUsernameOnlyAsync(
            "old.user",
            "someone.else",
            accounts,
            CancellationToken.None);

        Assert.Contains(createErrors, error => error.Code == AccountErrorCodes.UsernameRetired);
        Assert.Contains(renameErrors, error => error.Code == AccountErrorCodes.UsernameRetired);
    }

    [Fact]
    public async Task Disable_does_not_release_username()
    {
        var accounts = new InMemoryAccountRepository();
        var account = await accounts.CreateAsync(Account.Create("kept.user", "hash"));
        account.Disable();
        await accounts.UpdateAsync(account);

        Assert.NotNull(await accounts.FindByUsernameAsync("kept.user"));
        Assert.True(await accounts.IsUsernameTakenAsync("kept.user"));
        Assert.False(await accounts.IsUsernameRetiredAsync("kept.user"));

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            "kept.user",
            "password1",
            accounts,
            CancellationToken.None);

        Assert.Contains(errors, error => error.Code == AccountErrorCodes.UsernameAlreadyTaken);
    }
}

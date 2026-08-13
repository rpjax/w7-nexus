using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Shared;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class HandleIdentityTests
{
    [Fact]
    public async Task Create_rejects_handle_already_in_use()
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
    public async Task Create_rejects_retired_handle()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.RetireHandleAsync("alice", AccountId.New());

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            "alice",
            "password1",
            accounts,
            CancellationToken.None);

        Assert.Contains(errors, error => error.Code == AccountErrorCodes.HandleRetired);
        Assert.True(await accounts.IsHandleTakenAsync("alice"));
    }

    [Fact]
    public async Task Rename_reserves_previous_handle_and_blocks_reuse()
    {
        var accounts = new InMemoryAccountRepository();
        var account = await accounts.CreateAsync(Account.Create("old.handle", "hash"));
        account.ChangeUsername("new.handle");
        await accounts.UpdateChangingHandleAsync(account, "old.handle");

        Assert.Null(await accounts.FindByUsernameAsync("old.handle"));
        Assert.NotNull(await accounts.FindByUsernameAsync("new.handle"));
        Assert.True(await accounts.IsHandleRetiredAsync("old.handle"));

        var createErrors = await AccountRegistrationPolicy.ValidateAsync(
            "old.handle",
            "password1",
            accounts,
            CancellationToken.None);
        var renameErrors = await AccountRegistrationPolicy.ValidateUsernameOnlyAsync(
            "old.handle",
            "someone.else",
            accounts,
            CancellationToken.None);

        Assert.Contains(createErrors, error => error.Code == AccountErrorCodes.HandleRetired);
        Assert.Contains(renameErrors, error => error.Code == AccountErrorCodes.HandleRetired);
    }

    [Fact]
    public async Task Disable_does_not_release_handle()
    {
        var accounts = new InMemoryAccountRepository();
        var account = await accounts.CreateAsync(Account.Create("kept.handle", "hash"));
        account.Disable();
        await accounts.UpdateAsync(account);

        Assert.NotNull(await accounts.FindByUsernameAsync("kept.handle"));
        Assert.True(await accounts.IsHandleTakenAsync("kept.handle"));
        Assert.False(await accounts.IsHandleRetiredAsync("kept.handle"));

        var errors = await AccountRegistrationPolicy.ValidateAsync(
            "kept.handle",
            "password1",
            accounts,
            CancellationToken.None);

        Assert.Contains(errors, error => error.Code == AccountErrorCodes.UsernameAlreadyTaken);
    }
}

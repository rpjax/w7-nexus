using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Accounts.Domain.Events;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class AccountDomainTests
{
    [Fact]
    public void Disable_twice_fails_and_does_not_append()
    {
        var account = Account.Create("alice", "hash");
        Assert.True(account.Disable().IsSuccess);
        var count = account.UncommittedEvents.Count;
        var again = account.Disable();
        Assert.True(again.IsFailure);
        Assert.Equal(AccountErrorCodes.AccountAlreadyDisabled, again.Errors.First().Code);
        Assert.Equal(count, account.UncommittedEvents.Count);
    }

    [Fact]
    public void Enable_requires_disabled()
    {
        var account = Account.Create("alice", "hash");
        var result = account.Enable();
        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrorCodes.AccountAlreadyActive, result.Errors.First().Code);
    }

    [Fact]
    public void Password_change_is_marker_without_hash()
    {
        var account = Account.Create("alice", "secret-hash");
        Assert.True(account.ChangePassword("new-hash").IsSuccess);
        Assert.Equal("new-hash", account.PasswordHash);
        var marker = Assert.Single(account.UncommittedEvents.OfType<AccountPasswordChanged>());
        Assert.DoesNotContain("new-hash", marker.ToString());
        Assert.DoesNotContain("secret-hash", string.Join("|", account.UncommittedEvents.Select(e => e.ToString())));
    }

    [Fact]
    public void Username_unchanged_fails_without_event()
    {
        var account = Account.Create("alice", "hash");
        var count = account.UncommittedEvents.Count;
        var result = account.ChangeUsername("ALICE");
        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrorCodes.UsernameUnchanged, result.Errors.First().Code);
        Assert.Equal(count, account.UncommittedEvents.Count);
    }

    [Fact]
    public void Grant_and_revoke_administrator_round_trip()
    {
        var account = Account.Create("alice", "hash");
        Assert.False(account.IsAdministrator);
        Assert.True(account.AddRole(Roles.Administrator).IsSuccess);
        Assert.True(account.IsAdministrator);
        Assert.True(account.RemoveRole(Roles.Administrator).IsSuccess);
        Assert.False(account.IsAdministrator);
    }

    [Fact]
    public void Save_reload_preserves_state_except_password_hash()
    {
        var live = Account.Create("alice", "hash", [Roles.Administrator]);
        Assert.True(live.ChangeUsername("alice2").IsSuccess);
        Assert.True(live.Disable().IsSuccess);
        Assert.True(live.Enable().IsSuccess);
        Assert.True(live.ChangePassword("other").IsSuccess);
        Assert.True(live.AddPermission("beta").IsSuccess);

        var bag = new EventStreamBag();
        bag.Append(live.Id.Value, live.UncommittedEvents);
        live.ClearUncommitted();

        var reloaded = bag.Load<Account>(live.Id.Value)!;
        Assert.Equal(live.Id, reloaded.Id);
        Assert.Equal("alice2", reloaded.Username);
        Assert.Equal(AccountStatus.Active, reloaded.Status);
        Assert.True(reloaded.IsAdministrator);
        Assert.Contains("beta", reloaded.Permissions);
        Assert.True(string.IsNullOrEmpty(reloaded.PasswordHash));
    }
}

using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignIn;
using Refactor.Nexus.Api.Authentication.Domain.Errors;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class SignInDisabledTests
{
    [Fact]
    public async Task Sign_in_rejects_disabled_account()
    {
        var accounts = new InMemoryAccountRepository();
        var account = Account.Create("disabled.user", "hash:password1");
        account.Disable();
        await accounts.CreateAsync(account);

        var handler = new SignInHandler(accounts, new FakePasswordVerifier(), new FakeJwtTokenService());
        var result = await handler.HandleAsync(new SignInCommand("disabled.user", "password1"));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == AuthenticationErrorCodes.AccountDisabled);
    }

    [Fact]
    public async Task Sign_in_accepts_active_account()
    {
        var accounts = new InMemoryAccountRepository();
        await accounts.CreateAsync(Account.Create("active.user", "hash:password1"));

        var handler = new SignInHandler(accounts, new FakePasswordVerifier(), new FakeJwtTokenService());
        var result = await handler.HandleAsync(new SignInCommand("active.user", "password1"));

        Assert.False(result.IsFailure);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Tokens.AccessToken));
    }
}

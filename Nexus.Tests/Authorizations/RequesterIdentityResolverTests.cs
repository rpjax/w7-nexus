using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Errors;
using Nexus.Authorizations.Application.Services;
using Nexus.Authorizations.Errors;
using Nexus.Tests.Accounts;
using Xunit;

namespace Nexus.Tests.Authorizations;

public sealed class RequesterIdentityResolverTests
{
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly FakeHttpContextAccessor _httpContextAccessor = new();

    [Fact]
    public async Task ResolveAsync_Unauthenticated_ReturnsIdentityRequired()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext();
        var sut = CreateSut();

        var result = await sut.ResolveAsync();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AuthorizationErrorCodes.IdentityRequired);
    }

    [Fact]
    public async Task ResolveAsync_MissingAccountIdClaim_ReturnsAccountIdClaimMissing()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"))
        };
        var sut = CreateSut();

        var result = await sut.ResolveAsync();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AuthorizationErrorCodes.AccountIdClaimMissing);
    }

    [Fact]
    public async Task ResolveAsync_AccountNotFound_ReturnsAccountNotFound()
    {
        SetAuthenticatedUser("missing-account");
        var sut = CreateSut();

        var result = await sut.ResolveAsync();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task ResolveAsync_ValidAccount_ReturnsIdentityWithRolesFromAccount()
    {
        await _accounts.CreateAsync(new Account(
            "user-1",
            "user-1",
            "hash",
            new[] { "administrator", "operator" },
            Array.Empty<string>()));
        SetAuthenticatedUser("user-1");
        var sut = CreateSut();

        var result = await sut.ResolveAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("user-1", result.Value.AccountId);
        Assert.Contains("administrator", result.Value.Roles);
        Assert.Contains("operator", result.Value.Roles);
    }

    private RequesterIdentityResolver CreateSut()
        => new(_httpContextAccessor, _accounts);

    private void SetAuthenticatedUser(string accountId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, accountId) },
            authenticationType: "Test");

        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}

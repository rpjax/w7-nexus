using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Application.Services;
using Nexus.Tests.Accounts;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Authorization;

public sealed class AccessResolverTests
{
    private readonly ActorTestContext _ctx = new();
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly FakeHttpContextAccessor _httpContextAccessor = new();

    [Fact]
    public async Task OperationAdministratorAccess_GlobalAdministrator_AuthorizedWithoutOperationAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var sut = CreateOperationAdministratorAccess();

        var result = await InvokeAsUser(
            adminId,
            () => sut.ResolveForOperationAsync(operation.Id));

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Role);
    }

    [Fact]
    public async Task OperationAdministratorAccess_NonAdministratorWithoutAssignment_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var operation = await _ctx.SeedOperationAsync();
        var sut = CreateOperationAdministratorAccess();

        var result = await InvokeAsUser(
            userId,
            () => sut.ResolveForOperationAsync(operation.Id));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task TeamLeaderAccess_GlobalAdministrator_AuthorizedWithoutTeamLeaderAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderAccess();

        var result = await InvokeAsUser(
            adminId,
            () => sut.ResolveForTeamAsync(team.Id));

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Role);
    }

    [Fact]
    public async Task TeamLeaderAccess_NonAdministratorWithoutAssignment_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderAccess();

        var result = await InvokeAsUser(
            userId,
            () => sut.ResolveForTeamAsync(team.Id));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task OperatorAccess_GlobalAdministrator_AuthorizedWithoutOperatorRole()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateOperatorAccess();

        var result = await InvokeAsUser(adminId, () => sut.ResolveAsync());

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Role);
    }

    [Fact]
    public async Task OperatorAccess_NonAdministratorWithoutOperatorRole_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateOperatorAccess();

        var result = await InvokeAsUser(userId, () => sut.ResolveAsync());

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    private OperationAdministratorAccess CreateOperationAdministratorAccess()
        => new(
            _httpContextAccessor,
            _accounts,
            _ctx.Operations,
            _ctx.Teams,
            _ctx.CreateOperationAdministrator());

    private TeamLeaderAccess CreateTeamLeaderAccess()
        => new(
            _httpContextAccessor,
            _accounts,
            _ctx.Teams,
            _ctx.CreateTeamLeader());

    private OperatorAccess CreateOperatorAccess()
        => new(
            _httpContextAccessor,
            _accounts,
            _ctx.CreateOperator("operator-1"));

    private async Task SeedAccountAsync(string accountId, params string[] roles)
    {
        await _accounts.CreateAsync(new Account(
            accountId,
            accountId,
            "hash",
            roles,
            Array.Empty<string>()));
    }

    private async Task<T> InvokeAsUser<T>(string accountId, Func<Task<T>> action)
    {
        _httpContextAccessor.HttpContext = CreateAuthenticatedContext(accountId);
        return await action();
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string accountId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, accountId) },
            authenticationType: "Test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}

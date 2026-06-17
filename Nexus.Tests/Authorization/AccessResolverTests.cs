using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrator.Application.Services;
using Nexus.Administrator.Application.Services;
using Nexus.TeamLeader.Application.Services;
using Nexus.Operator.Application.Services;
using Nexus.StrawMan.Application.Services;
using Nexus.Tests.Accounts;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Authorization;

public sealed class AccessResolverTests
{
    private readonly ActorTestContext _ctx = new();
    private readonly InMemoryAccountRepository _accounts = new();

    [Fact]
    public async Task OperationAdministratorPolicy_GlobalAdministrator_AuthorizedWithoutOperationAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: operation.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_NonAdministratorWithoutAssignment_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var operation = await _ctx.SeedOperationAsync();
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: operation.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task AdministratorPolicy_AccountWithAdministratorRole_Authorized()
    {
        const string adminId = "admin-1";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task AdministratorPolicy_GlobalAdministratorWithoutAdministratorRole_Unauthorized()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(adminId);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task AdministratorPolicy_NonAdministrator_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task TeamLeaderPolicy_GlobalAdministrator_AuthorizedWithoutTeamLeaderAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeManageTeamAsync(identity, team.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task TeamLeaderPolicy_NonAdministratorWithoutAssignment_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeManageTeamAsync(identity, team.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task OperatorPolicy_GlobalAdministrator_AuthorizedWithoutOperatorRole()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateOperatorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeSearchOperationsAsync(identity, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task OperatorPolicy_NonAdministratorWithoutOperatorRole_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateOperatorPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeSearchOperationsAsync(identity, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task StrawManPolicy_GlobalAdministrator_AuthorizedWithoutStrawManRole()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeStrawManAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task StrawManPolicy_AccountWithStrawManRole_Authorized()
    {
        const string strawManId = "strawman-1";
        await SeedAccountAsync(strawManId, Roles.StrawMan);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(strawManId, Roles.StrawMan);

        var result = await sut.AuthorizeStrawManAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public async Task StrawManPolicy_NonAdministratorWithoutStrawManRole_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeStrawManAsync(identity);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
    }

    private OperationAdministratorAccessPolicy CreateOperationAdministratorPolicy()
        => new(_ctx.Operations, _ctx.Teams);

    private static AdministratorAccessPolicy CreateAdministratorPolicy()
        => new();

    private TeamLeaderAccessPolicy CreateTeamLeaderPolicy()
        => new(_ctx.Teams);

    private static OperatorAccessPolicy CreateOperatorPolicy()
        => new();

    private static StrawManAccessPolicy CreateStrawManPolicy()
        => new();

    private async Task SeedAccountAsync(string accountId, params string[] roles)
    {
        await _accounts.CreateAsync(new Account(
            accountId,
            accountId,
            "hash",
            roles,
            Array.Empty<string>()));
    }

    private static RequesterIdentity CreateIdentity(string accountId, params string[] roles)
        => new(accountId, roles, Array.Empty<string>());
}

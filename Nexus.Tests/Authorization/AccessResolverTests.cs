using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.OperationAdministrator.Application.Services;
using Nexus.Administrator.Application.Services;
using Nexus.TeamLeader.Application.Services;
using Nexus.Operator.Application.Services;
using Nexus.StrawMan.Application.Services;
using Nexus.Operations.Errors;
using Nexus.Tests.Accounts;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Authorization;

public sealed class AccessResolverTests
{
    private readonly ActorTestContext _ctx = new();
    private readonly InMemoryAccountRepository _accounts = new();

    #region OperationAdministrator

    [Fact]
    public async Task OperationAdministratorPolicy_GlobalAdministrator_AuthorizedWithoutOperationAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: operation.Id);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_AssignedOpAdmin_AuthorizedToManageOperation()
    {
        const string opAdminId = "op-admin-1";
        var operation = await _ctx.SeedOperationAsync(administratorIds: [opAdminId]);
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(opAdminId);

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: operation.Id);

        AuthorizationTestHelpers.AssertAuthorized(result);
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

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_AssignedOpAdmin_UnauthorizedForOtherOperation()
    {
        const string opAdminId = "op-admin-1";
        await _ctx.SeedOperationAsync("Assigned", administratorIds: [opAdminId]);
        var other = await _ctx.SeedOperationAsync("Other", administratorIds: ["other-admin"]);
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(opAdminId);

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: other.Id);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_ManageByTeamId_AssignedOpAdmin_Authorized()
    {
        const string opAdminId = "op-admin-1";
        var operation = await _ctx.SeedOperationAsync(administratorIds: [opAdminId]);
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(opAdminId);

        var result = await sut.AuthorizeManageOperationAsync(identity, teamId: team.Id);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_ManageByTeamId_TeamNotFound_ReturnsFailure()
    {
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity("op-admin-1");

        var result = await sut.AuthorizeManageOperationAsync(identity, teamId: "missing-team");

        AuthorizationTestHelpers.AssertPolicyFailure(result, TeamErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_ManageOperation_EmptyOperationId_ReturnsFailure()
    {
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity("op-admin-1");

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: string.Empty);

        AuthorizationTestHelpers.AssertPolicyFailure(result, OperationErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_ManageOperation_OperationNotFound_ReturnsFailure()
    {
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity("op-admin-1");

        var result = await sut.AuthorizeManageOperationAsync(identity, operationId: "missing-operation");

        AuthorizationTestHelpers.AssertPolicyFailure(result, OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_SearchOperations_AssignedOpAdmin_Authorized()
    {
        const string opAdminId = "op-admin-1";
        await _ctx.SeedOperationAsync(administratorIds: [opAdminId]);
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity(opAdminId);

        var result = await sut.AuthorizeSearchOperationsAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_SearchOperations_UnassignedOpAdmin_Unauthorized()
    {
        await _ctx.SeedOperationAsync(administratorIds: ["other-admin"]);
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity("op-admin-1");

        var result = await sut.AuthorizeSearchOperationsAsync(identity);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task OperationAdministratorPolicy_SearchOperations_GlobalAdministrator_Authorized()
    {
        var sut = CreateOperationAdministratorPolicy();
        var identity = CreateIdentity("global-admin", Roles.Administrator);

        var result = await sut.AuthorizeSearchOperationsAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    #endregion

    #region Administrator

    [Fact]
    public async Task AdministratorPolicy_AccountWithAdministratorRole_Authorized()
    {
        const string adminId = "admin-1";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task AdministratorPolicy_GlobalAdministratorWithoutAdministratorRole_Unauthorized()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(adminId);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotAdministrator);
    }

    [Fact]
    public async Task AdministratorPolicy_NonAdministrator_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateAdministratorPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeAdministratorAsync(identity);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotAdministrator);
    }

    #endregion

    #region TeamLeader

    [Fact]
    public async Task TeamLeaderPolicy_SearchLedTeams_GlobalAdministrator_Authorized()
    {
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("global-admin", Roles.Administrator);

        var result = await sut.AuthorizeSearchLedTeamsAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task TeamLeaderPolicy_SearchLedTeams_TeamLeaderLeadsAnyTeam_Authorized()
    {
        var operation = await _ctx.SeedOperationAsync();
        await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "team-leader-1");
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("team-leader-1");

        var result = await sut.AuthorizeSearchLedTeamsAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task TeamLeaderPolicy_SearchLedTeams_NonTeamLeader_Unauthorized()
    {
        var operation = await _ctx.SeedOperationAsync();
        await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "other-leader");
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("team-leader-1");

        var result = await sut.AuthorizeSearchLedTeamsAsync(identity);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotTeamLeader);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_GlobalAdministrator_AuthorizedWithoutTeamLeaderAssignment()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeManageTeamAsync(identity, team.Id);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_TeamLeaderOfTeam_AuthorizedToOwnTeam()
    {
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "team-leader-1");
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("team-leader-1");

        var result = await sut.AuthorizeManageTeamAsync(identity, team.Id);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_TeamLeaderOfOtherTeam_Unauthorized()
    {
        var operation = await _ctx.SeedOperationAsync();
        await _ctx.SeedTeamAsync(operation.Id, name: "Team A", teamLeaderId: "leader-a");
        var teamB = await _ctx.SeedTeamAsync(operation.Id, name: "Team B", teamLeaderId: "leader-b");
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("leader-a");

        var result = await sut.AuthorizeManageTeamAsync(identity, teamB.Id);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotTeamLeader);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_NonAdministratorWithoutAssignment_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var operation = await _ctx.SeedOperationAsync();
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeManageTeamAsync(identity, team.Id);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotTeamLeader);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_EmptyTeamId_ReturnsFailure()
    {
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("team-leader-1");

        var result = await sut.AuthorizeManageTeamAsync(identity, string.Empty);

        AuthorizationTestHelpers.AssertPolicyFailure(result, TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task TeamLeaderPolicy_ManageTeam_TeamNotFound_ReturnsFailure()
    {
        var sut = CreateTeamLeaderPolicy();
        var identity = CreateIdentity("team-leader-1");

        var result = await sut.AuthorizeManageTeamAsync(identity, "missing-team");

        AuthorizationTestHelpers.AssertPolicyFailure(result, TeamErrorCodes.TeamNotFound);
    }

    #endregion

    #region Operator

    [Fact]
    public async Task OperatorPolicy_GlobalAdministrator_AuthorizedWithoutOperatorRole()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateOperatorPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeSearchOperationsAsync(identity, CancellationToken.None);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task OperatorPolicy_AccountWithOperatorRole_Authorized()
    {
        const string operatorId = "operator-1";
        await SeedAccountAsync(operatorId, Roles.Operator);
        var sut = CreateOperatorPolicy();
        var identity = CreateIdentity(operatorId, Roles.Operator);

        var result = await sut.AuthorizeSearchOperationsAsync(identity, CancellationToken.None);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task OperatorPolicy_NonAdministratorWithoutOperatorRole_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateOperatorPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeSearchOperationsAsync(identity, CancellationToken.None);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotOperator);
    }

    #endregion

    #region StrawMan

    [Fact]
    public async Task StrawManPolicy_GlobalAdministrator_AuthorizedWithoutStrawManRole()
    {
        const string adminId = "global-admin";
        await SeedAccountAsync(adminId, Roles.Administrator);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(adminId, Roles.Administrator);

        var result = await sut.AuthorizeStrawManAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task StrawManPolicy_AccountWithStrawManRole_Authorized()
    {
        const string strawManId = "strawman-1";
        await SeedAccountAsync(strawManId, Roles.StrawMan);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(strawManId, Roles.StrawMan);

        var result = await sut.AuthorizeStrawManAsync(identity);

        AuthorizationTestHelpers.AssertAuthorized(result);
    }

    [Fact]
    public async Task StrawManPolicy_NonAdministratorWithoutStrawManRole_Unauthorized()
    {
        const string userId = "regular-user";
        await SeedAccountAsync(userId);
        var sut = CreateStrawManPolicy();
        var identity = CreateIdentity(userId);

        var result = await sut.AuthorizeStrawManAsync(identity);

        AuthorizationTestHelpers.AssertUnauthorized(result, AuthorizationErrorCodes.NotStrawMan);
    }

    #endregion

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

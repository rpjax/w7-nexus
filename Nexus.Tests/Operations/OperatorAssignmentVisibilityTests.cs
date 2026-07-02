using Aidan.Core.Linq.Extensions;
using Nexus.Operations.Application.Requests.Administrator;
using AdminAssignOperatorToTeamRequest = Nexus.Operations.Application.Requests.Administrator.AssignOperatorToTeamRequest;
using AdminSearchOperatorsToAssignRequest = Nexus.Operations.Application.Requests.Administrator.SearchOperatorsToAssignRequest;
using TeamLeaderSearchOperatorsToAssignRequest = Nexus.Operations.Application.Requests.TeamLeader.SearchOperatorsToAssignRequest;
using Nexus.Tests.Support;
using Xunit;
using Nexus.Authorization;

namespace Nexus.Tests.Operations;

public sealed class OperatorAssignmentVisibilityTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task TeamLeader_SearchOperatorsToAssign_OnlyReturnsOperatorsInSameOperation()
    {
        const string inOperationOperatorId = "op-in-operation";
        const string outsideOperatorId = "op-outside-operation";
        const string teamLeaderId = "leader-1";

        await _ctx.SeedAccountAsync("in-op", id: inOperationOperatorId, roles: [Roles.Operator]);
        await _ctx.SeedAccountAsync("outside", id: outsideOperatorId, roles: [Roles.Operator]);
        _ctx.RegisterAccount(inOperationOperatorId);
        _ctx.RegisterAccount(outsideOperatorId);
        _ctx.RegisterAccount(teamLeaderId);

        var operation = await _ctx.SeedOperationAsync("Op A");
        var otherOperation = await _ctx.SeedOperationAsync("Op B");
        var targetTeam = await _ctx.SeedTeamAsync(
            operation.Id,
            teamLeaderId: teamLeaderId,
            operatorIds: [inOperationOperatorId]);
        await _ctx.SeedTeamAsync(otherOperation.Id, operatorIds: [outsideOperatorId]);

        var teamLeader = _ctx.CreateTeamLeader();
        var identity = _ctx.CreateRequesterIdentity(teamLeaderId);

        var result = await teamLeader.SearchOperatorsToAssignAsync(identity, new TeamLeaderSearchOperatorsToAssignRequest
        {
            TeamId = targetTeam.Id,
            Limit = 20,
            Offset = 0,
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!.Items);
        Assert.Equal(inOperationOperatorId, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task Administrator_SearchOperatorsToAssign_ReturnsAllOperatorsInSystem()
    {
        const string inOperationOperatorId = "op-in-operation";
        const string outsideOperatorId = "op-outside-operation";

        await _ctx.SeedAccountAsync("in-op", id: inOperationOperatorId, roles: [Roles.Operator]);
        await _ctx.SeedAccountAsync("outside", id: outsideOperatorId, roles: [Roles.Operator]);
        _ctx.RegisterAccount(inOperationOperatorId);
        _ctx.RegisterAccount(outsideOperatorId);

        var operation = await _ctx.SeedOperationAsync("Op A");
        var otherOperation = await _ctx.SeedOperationAsync("Op B");
        await _ctx.SeedTeamAsync(operation.Id, operatorIds: [inOperationOperatorId]);
        await _ctx.SeedTeamAsync(otherOperation.Id, operatorIds: [outsideOperatorId]);

        var administrator = _ctx.CreateAdministrator();
        var identity = _ctx.CreateRequesterIdentity("admin-1", additionalRoles: Roles.Administrator);

        var result = await administrator.SearchOperatorsToAssignAsync(identity, new AdminSearchOperatorsToAssignRequest
        {
            Limit = 20,
            Offset = 0,
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Total);
        Assert.Contains(result.Value.Items, item => item.Id == inOperationOperatorId);
        Assert.Contains(result.Value.Items, item => item.Id == outsideOperatorId);
    }

    [Fact]
    public async Task Administrator_AssignOperatorToTeam_UsesAdministratorActorNotTeamLeaderBypass()
    {
        const string operatorId = "operator-1";
        const string adminId = "admin-1";

        await _ctx.SeedAccountAsync("operator", id: operatorId, roles: [Roles.Operator]);
        _ctx.RegisterAccount(operatorId);

        var operation = await _ctx.SeedOperationAsync("Op A");
        var team = await _ctx.SeedTeamAsync(operation.Id);

        var administrator = _ctx.CreateAdministrator();
        var identity = _ctx.CreateRequesterIdentity(adminId, additionalRoles: Roles.Administrator);

        var result = await administrator.AssignOperatorToTeamAsync(identity, new AdminAssignOperatorToTeamRequest
        {
            TeamId = team.Id,
            OperatorId = operatorId,
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);

        var updatedTeam = await _ctx.Teams.AsQueryable()
            .Where(t => t.Id == team.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(updatedTeam);
        Assert.Contains(operatorId, updatedTeam!.OperatorIds);
    }
}

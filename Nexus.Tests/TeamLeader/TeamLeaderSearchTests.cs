using Nexus.Authorization.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.TeamLeader.Application.Requests;
using Nexus.Authorization.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.TeamLeader;

public sealed class TeamLeaderSearchTests
{
    private readonly ActorTestContext _ctx = new();

    private RequesterIdentity Identity(string accountId = "team-leader-1")
        => _ctx.CreateRequesterIdentity(accountId);

    [Fact]
    public async Task SearchLedTeamsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var operation = await _ctx.SeedOperationAsync();
        await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "team-leader-1");
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SearchLedTeamsAsync(Identity(), default(SearchLedTeamsRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchLedTeamsAsync_TeamLeaderSeesOnlyLedTeamsGroupedByOperation()
    {
        var operationA = await _ctx.SeedOperationAsync("Operation A");
        var operationB = await _ctx.SeedOperationAsync("Operation B");
        await _ctx.SeedTeamAsync(operationA.Id, name: "Team A1", teamLeaderId: "team-leader-1");
        await _ctx.SeedTeamAsync(operationA.Id, name: "Team A2", teamLeaderId: "team-leader-1");
        await _ctx.SeedTeamAsync(operationB.Id, name: "Team B1", teamLeaderId: "team-leader-1");
        await _ctx.SeedTeamAsync(operationB.Id, name: "Other Team", teamLeaderId: "other-leader");
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SearchLedTeamsAsync(Identity("team-leader-1"), new SearchLedTeamsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.Total);

        var opA = result.Value.Items.Single(i => i.Name == "Operation A");
        Assert.Equal(2, opA.Teams.Length);
        Assert.DoesNotContain(opA.Teams, t => t.Name == "Other Team");

        var opB = result.Value.Items.Single(i => i.Name == "Operation B");
        Assert.Single(opB.Teams);
        Assert.Equal("Team B1", opB.Teams[0].Name);
    }

    [Fact]
    public async Task SearchLedTeamsAsync_NonTeamLeader_Unauthorized()
    {
        var operation = await _ctx.SeedOperationAsync("Operation A");
        await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "other-leader");
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SearchLedTeamsAsync(Identity("team-leader-1"), new SearchLedTeamsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotTeamLeader);
    }

    [Fact]
    public async Task SearchLedTeamsAsync_KeywordFilter_FiltersByOperationName()
    {
        var alpha = await _ctx.SeedOperationAsync("Alpha Operation");
        var beta = await _ctx.SeedOperationAsync("Beta Operation");
        await _ctx.SeedTeamAsync(alpha.Id, teamLeaderId: "team-leader-1");
        await _ctx.SeedTeamAsync(beta.Id, teamLeaderId: "team-leader-1");
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SearchLedTeamsAsync(Identity("team-leader-1"), new SearchLedTeamsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = "alpha"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("Alpha Operation", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task SearchLedTeamsAsync_Pagination_PaginatesByOperation()
    {
        var sut = _ctx.CreateTeamLeader();
        for (var i = 0; i < 5; i++)
        {
            var operation = await _ctx.SeedOperationAsync($"Operation {i:D2}");
            await _ctx.SeedTeamAsync(operation.Id, teamLeaderId: "team-leader-1");
        }

        var result = await sut.SearchLedTeamsAsync(Identity("team-leader-1"), new SearchLedTeamsRequest
        {
            Limit = 2,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.Total);
    }

    [Fact]
    public async Task SearchLedTeamsAsync_ReturnsEnrichedTeamOperators()
    {
        var operation = await _ctx.SeedOperationAsync("Enriched Operation");
        await _ctx.SeedTeamAsync(
            operation.Id,
            teamLeaderId: "team-leader-1",
            operatorIds: new[] { "operator-1" });
        await _ctx.SeedAccountAsync("leader", id: "team-leader-1");
        await _ctx.SeedAccountAsync("operator1", id: "operator-1");
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SearchLedTeamsAsync(Identity("team-leader-1"), new SearchLedTeamsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var item = Assert.Single(result.Value.Items);
        var team = Assert.Single(item.Teams);
        Assert.Equal("leader", team.TeamLeader!.Username);
        Assert.Single(team.Operators);
        Assert.Equal("operator1", team.Operators[0].Username);
    }
}

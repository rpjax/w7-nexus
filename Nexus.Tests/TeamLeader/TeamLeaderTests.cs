using Nexus.TeamLeader.Application.Requests;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.TeamLeader;

public sealed class TeamLeaderTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task AssignOperatorToTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.AssignOperatorToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignOperatorToTeamAsync_ValidRequest_AssignsOperator()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("operator-1");

        var result = await sut.AssignOperatorToTeamAsync(new AssignOperatorToTeamRequest
        {
            TeamId = teamId,
            OperatorId = "operator-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains("operator-1", team.OperatorIds);
    }

    [Fact]
    public async Task UnassignOperatorFromTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.UnassignOperatorFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignOperatorFromTeamAsync_ValidRequest_UnassignsOperator()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("operator-1");
        await sut.AssignOperatorToTeamAsync(new AssignOperatorToTeamRequest
        {
            TeamId = teamId,
            OperatorId = "operator-1"
        });

        var result = await sut.UnassignOperatorFromTeamAsync(new UnassignOperatorFromTeamRequest
        {
            TeamId = teamId,
            OperatorId = "operator-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain("operator-1", team.OperatorIds);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SetOperatorProfitShareRuleAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_ValidRequest_SetsRule()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("operator-1");
        _ctx.RegisterAccount("payee-1");
        await sut.AssignOperatorToTeamAsync(new AssignOperatorToTeamRequest
        {
            TeamId = teamId,
            OperatorId = "operator-1"
        });

        var result = await sut.SetOperatorProfitShareRuleAsync(new SetOperatorProfitShareRuleRequest
        {
            TeamId = teamId,
            OperatorId = "operator-1",
            Cuts =
            [
                new ProfitShareCutRequest { AccountId = " payee-1 ", Percentage = 100m }
            ]
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.True(team.OperatorProfitShareRules.Any(r => r.OperatorId == "operator-1"));
        Assert.Equal(100m, team.OperatorProfitShareRules.First(r => r.OperatorId == "operator-1").Cuts.First(c => c.AccountId == "payee-1").Percentage);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_OperatorNotOnTeam_PropagatesError()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("payee-1");

        var result = await sut.SetOperatorProfitShareRuleAsync(new SetOperatorProfitShareRuleRequest
        {
            TeamId = teamId,
            OperatorId = "missing-operator",
            Cuts =
            [
                new ProfitShareCutRequest { AccountId = "payee-1", Percentage = 100m }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorNotAssigned);
    }

    private async Task<(string TeamId, string OperationId)> SeedTeamAsync()
    {
        var operation = await _ctx.SeedOperationAsync("Team Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id);
        return (team.Id, operation.Id);
    }
}

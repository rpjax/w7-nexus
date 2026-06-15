using Nexus.TeamLeader.Application.Requests;
using Nexus.Operations.Aggregates;
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
    public async Task SetTeamGatewaySelectionStrategyAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.SetTeamGatewaySelectionStrategyAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SetTeamGatewaySelectionStrategyAsync_ValidRequest_UpdatesStrategy()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();

        var result = await sut.SetTeamGatewaySelectionStrategyAsync(new SetTeamGatewaySelectionStrategyRequest
        {
            TeamId = teamId,
            Strategy = GatewaySelectionStrategy.Manual
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Equal(GatewaySelectionStrategy.Manual, team.GatewaySelectionStrategy);
    }

    [Fact]
    public async Task AssignStrawManToTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.AssignStrawManToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignStrawManToTeamAsync_ValidRequest_AssignsStrawMan()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("straw-1");

        var result = await sut.AssignStrawManToTeamAsync(new AssignStrawManToTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains("straw-1", team.StrawManIds);
    }

    [Fact]
    public async Task UnassignStrawManFromTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.UnassignStrawManFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignStrawManFromTeamAsync_ValidRequest_UnassignsStrawMan()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync();
        _ctx.RegisterAccount("straw-1");
        await sut.AssignStrawManToTeamAsync(new AssignStrawManToTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        var result = await sut.UnassignStrawManFromTeamAsync(new UnassignStrawManFromTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain("straw-1", team.StrawManIds);
    }

    [Fact]
    public async Task AssignGatewayAccountGroupToTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.AssignGatewayAccountGroupToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignGatewayAccountGroupToTeamAsync_ValidRequest_AssignsGroup()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.PerGroup);
        var group = await _ctx.SeedGatewayGroupAsync();

        var result = await sut.AssignGatewayAccountGroupToTeamAsync(new AssignGatewayAccountGroupToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains(group.Id, team.GatewayCredentialsGroupIds);
    }

    [Fact]
    public async Task UnassignGatewayAccountGroupFromTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.UnassignGatewayAccountGroupFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignGatewayAccountGroupFromTeamAsync_ValidRequest_UnassignsGroup()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.PerGroup);
        var group = await _ctx.SeedGatewayGroupAsync();
        await sut.AssignGatewayAccountGroupToTeamAsync(new AssignGatewayAccountGroupToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        var result = await sut.UnassignGatewayAccountGroupFromTeamAsync(new UnassignGatewayAccountGroupFromTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain(group.Id, team.GatewayCredentialsGroupIds);
    }

    [Fact]
    public async Task AssignGatewayAccountToTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.AssignGatewayAccountToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignGatewayAccountToTeamAsync_ValidRequest_AssignsCredential()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.Manual);
        _ctx.RegisterGatewayCredential("cred-1");

        var result = await sut.AssignGatewayAccountToTeamAsync(new AssignGatewayAccountToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains("cred-1", team.GatewayCredentialsIds);
    }

    [Fact]
    public async Task UnassignGatewayAccountFromTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateTeamLeader();

        var result = await sut.UnassignGatewayAccountFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignGatewayAccountFromTeamAsync_ValidRequest_UnassignsCredential()
    {
        var sut = _ctx.CreateTeamLeader();
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.Manual);
        _ctx.RegisterGatewayCredential("cred-1");
        await sut.AssignGatewayAccountToTeamAsync(new AssignGatewayAccountToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        var result = await sut.UnassignGatewayAccountFromTeamAsync(new UnassignGatewayAccountFromTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain("cred-1", team.GatewayCredentialsIds);
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

    private async Task<(string TeamId, string OperationId)> SeedTeamAsync(
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman)
    {
        var operation = await _ctx.SeedOperationAsync("Team Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id, strategy: strategy);
        return (team.Id, operation.Id);
    }
}

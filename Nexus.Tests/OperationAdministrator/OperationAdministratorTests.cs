using Nexus.OperationAdministrator.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.OperationAdministrator;

public sealed class OperationAdministratorTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task CreateOperationTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task CreateOperationTeamAsync_ValidRequest_ReturnsTeamDetails()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");

        var result = await sut.CreateOperationTeamAsync(new CreateOperationTeamRequest
        {
            OperationId = operation.Id,
            Name = "Alpha Team"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Team);
        Assert.Equal("Alpha Team", result.Value!.Team.Name);
        Assert.Equal(operation.Id, result.Value.Team.OperationId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Team.Id));
    }

    [Fact]
    public async Task CreateOperationTeamAsync_OperationNotFound_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(new CreateOperationTeamRequest
        {
            OperationId = "missing-operation",
            Name = "Alpha Team"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_ValidRequest_DeletesTeam()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id, "Disposable Team");

        var result = await sut.DeleteOperationTeamAsync(new DeleteOperationTeamRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(_ctx.Teams.AsQueryable().ToArray());
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_TeamNotFound_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(new DeleteOperationTeamRequest
        {
            TeamId = "missing-team"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignOperationTeamLeaderAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_ValidRequest_AssignsLeader()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id);
        _ctx.RegisterAccount("leader-1");

        var result = await sut.AssignOperationTeamLeaderAsync(new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "leader-1"
        });

        Assert.True(result.IsSuccess);
        var updated = _ctx.Teams.AsQueryable().First(t => t.Id == team.Id);
        Assert.Equal("leader-1", updated.TeamLeaderId);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_LeaderNotFound_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id);

        var result = await sut.AssignOperationTeamLeaderAsync(new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "missing-leader"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderAccountNotFound);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignOperationTeamLeaderAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_ValidRequest_UnassignsLeader()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id);
        _ctx.RegisterAccount("leader-1");
        await sut.AssignOperationTeamLeaderAsync(new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "leader-1"
        });

        var result = await sut.UnassignOperationTeamLeaderAsync(new UnassignOperationTeamLeaderRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsSuccess);
        var updated = _ctx.Teams.AsQueryable().First(t => t.Id == team.Id);
        Assert.Null(updated.TeamLeaderId);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_NoLeaderAssigned_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();
        var operation = await _ctx.SeedOperationAsync("Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id);

        var result = await sut.UnassignOperationTeamLeaderAsync(new UnassignOperationTeamLeaderRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderNotAssigned);
    }

    [Fact]
    public async Task SetTeamGatewaySelectionStrategyAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.SetTeamGatewaySelectionStrategyAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SetTeamGatewaySelectionStrategyAsync_ValidRequest_UpdatesStrategy()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignStrawManToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignStrawManToTeamAsync_ValidRequest_AssignsStrawMan()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignStrawManFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignStrawManFromTeamAsync_ValidRequest_UnassignsStrawMan()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignGatewayAccountGroupToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignGatewayAccountGroupToTeamAsync_ValidRequest_AssignsGroup()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignGatewayAccountGroupFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignGatewayAccountGroupFromTeamAsync_ValidRequest_UnassignsGroup()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignGatewayAccountToTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignGatewayAccountToTeamAsync_ValidRequest_AssignsCredential()
    {
        var sut = _ctx.CreateOperationAdministrator();
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
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignGatewayAccountFromTeamAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignGatewayAccountFromTeamAsync_ValidRequest_UnassignsCredential()
    {
        var sut = _ctx.CreateOperationAdministrator();
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

    private async Task<(string TeamId, string OperationId)> SeedTeamAsync(
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman)
    {
        var operation = await _ctx.SeedOperationAsync("Team Parent Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id, strategy: strategy);
        return (team.Id, operation.Id);
    }
}

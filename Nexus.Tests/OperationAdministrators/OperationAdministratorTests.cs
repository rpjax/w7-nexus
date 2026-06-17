using Nexus.Authorizations.Application.Models;
using Nexus.Authorizations.Errors;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.OperationAdministrators;

public sealed class OperationAdministratorTests
{
    private readonly ActorTestContext _ctx = new();
    private readonly RequesterIdentity _identity;

    public OperationAdministratorTests()
    {
        _identity = _ctx.CreateRequesterIdentity();
    }

    [Fact]
    public async Task CreateOperationTeamAsync_NullRequest_ReturnsOperationIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(_identity, default(CreateOperationTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task CreateOperationTeamAsync_UnassignedOpAdmin_ReturnsUnauthorized()
    {
        var operation = await _ctx.SeedOperationAsync("Other Operation", administratorIds: ["other-admin"]);
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(_identity, new CreateOperationTeamRequest
        {
            OperationId = operation.Id,
            Name = "Denied Team"
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_UnassignedOpAdmin_ReturnsUnauthorized()
    {
        var operation = await _ctx.SeedOperationAsync("Other Operation", administratorIds: ["other-admin"]);
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(_identity, new DeleteOperationTeamRequest
        {
            TeamId = team.Id
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_UnassignedOpAdmin_ReturnsUnauthorized()
    {
        var operation = await _ctx.SeedOperationAsync("Other Operation", administratorIds: ["other-admin"]);
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterAccount("leader-1");

        var result = await sut.AssignOperationTeamLeaderAsync(_identity, new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "leader-1"
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperationAdministrator);
    }

    [Fact]
    public async Task CreateOperationTeamAsync_ValidRequest_ReturnsTeamDetails()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(_identity, new CreateOperationTeamRequest
        {
            OperationId = operation.Id,
            Name = "Alpha Team"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Team);
        Assert.Equal("Alpha Team", result.Value.Team.Name);
        Assert.Equal(operation.Id, result.Value.Team.OperationId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Team.Id));
    }

    [Fact]
    public async Task CreateOperationTeamAsync_OperationNotFound_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.CreateOperationTeamAsync(_identity, new CreateOperationTeamRequest
        {
            OperationId = "missing-operation",
            Name = "Alpha Team"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(_identity, default(DeleteOperationTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_ValidRequest_DeletesTeam()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id, "Disposable Team");
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(_identity, new DeleteOperationTeamRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.Empty(_ctx.Teams.AsQueryable().ToArray());
    }

    [Fact]
    public async Task DeleteOperationTeamAsync_TeamNotFound_PropagatesError()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.DeleteOperationTeamAsync(_identity, new DeleteOperationTeamRequest
        {
            TeamId = "missing-team"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignOperationTeamLeaderAsync(_identity, default(AssignOperationTeamLeaderRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_ValidRequest_AssignsLeader()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterAccount("leader-1");

        var result = await sut.AssignOperationTeamLeaderAsync(_identity, new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "leader-1"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var updated = _ctx.Teams.AsQueryable().First(t => t.Id == team.Id);
        Assert.Equal("leader-1", updated.TeamLeaderId);
    }

    [Fact]
    public async Task AssignOperationTeamLeaderAsync_LeaderNotFound_PropagatesError()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignOperationTeamLeaderAsync(_identity, new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "missing-leader"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderAccountNotFound);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignOperationTeamLeaderAsync(_identity, default(UnassignOperationTeamLeaderRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_ValidRequest_UnassignsLeader()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterAccount("leader-1");
        await sut.AssignOperationTeamLeaderAsync(_identity, new AssignOperationTeamLeaderRequest
        {
            TeamId = team.Id,
            TeamLeaderId = "leader-1"
        });

        var result = await sut.UnassignOperationTeamLeaderAsync(_identity, new UnassignOperationTeamLeaderRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var updated = _ctx.Teams.AsQueryable().First(t => t.Id == team.Id);
        Assert.Null(updated.TeamLeaderId);
    }

    [Fact]
    public async Task UnassignOperationTeamLeaderAsync_NoLeaderAssigned_PropagatesError()
    {
        var operation = await _ctx.SeedOperationAsync("Parent Operation", administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id);
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignOperationTeamLeaderAsync(_identity, new UnassignOperationTeamLeaderRequest
        {
            TeamId = team.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderNotAssigned);
    }

    [Fact]
    public async Task SetTeamGatewaySelectionStrategyAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.SetTeamGatewaySelectionStrategyAsync(_identity, default(SetTeamGatewaySelectionStrategyRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task SetTeamGatewaySelectionStrategyAsync_ValidRequest_UpdatesStrategy()
    {
        var (teamId, _) = await SeedTeamAsync();
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.SetTeamGatewaySelectionStrategyAsync(_identity, new SetTeamGatewaySelectionStrategyRequest
        {
            TeamId = teamId,
            Strategy = GatewaySelectionStrategy.Manual
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Equal(GatewaySelectionStrategy.Manual, team.GatewaySelectionStrategy);
    }

    [Fact]
    public async Task AssignStrawManToTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignStrawManToTeamAsync(_identity, default(AssignStrawManToTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task AssignStrawManToTeamAsync_ValidRequest_AssignsStrawMan()
    {
        var (teamId, _) = await SeedTeamAsync();
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterAccount("straw-1");

        var result = await sut.AssignStrawManToTeamAsync(_identity, new AssignStrawManToTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains("straw-1", team.StrawManIds);
    }

    [Fact]
    public async Task UnassignStrawManFromTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignStrawManFromTeamAsync(_identity, default(UnassignStrawManFromTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task UnassignStrawManFromTeamAsync_ValidRequest_UnassignsStrawMan()
    {
        var (teamId, _) = await SeedTeamAsync();
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterAccount("straw-1");
        await sut.AssignStrawManToTeamAsync(_identity, new AssignStrawManToTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        var result = await sut.UnassignStrawManFromTeamAsync(_identity, new UnassignStrawManFromTeamRequest
        {
            TeamId = teamId,
            StrawManId = "straw-1"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain("straw-1", team.StrawManIds);
    }

    [Fact]
    public async Task AssignGatewayAccountGroupToTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignGatewayAccountGroupToTeamAsync(_identity, default(AssignGatewayAccountGroupToTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task AssignGatewayAccountGroupToTeamAsync_ValidRequest_AssignsGroup()
    {
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = _ctx.CreateOperationAdministrator();
        var group = await _ctx.SeedGatewayGroupAsync();

        var result = await sut.AssignGatewayAccountGroupToTeamAsync(_identity, new AssignGatewayAccountGroupToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains(group.Id, team.GatewayCredentialsGroupIds);
    }

    [Fact]
    public async Task UnassignGatewayAccountGroupFromTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignGatewayAccountGroupFromTeamAsync(_identity, default(UnassignGatewayAccountGroupFromTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task UnassignGatewayAccountGroupFromTeamAsync_ValidRequest_UnassignsGroup()
    {
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = _ctx.CreateOperationAdministrator();
        var group = await _ctx.SeedGatewayGroupAsync();
        await sut.AssignGatewayAccountGroupToTeamAsync(_identity, new AssignGatewayAccountGroupToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        var result = await sut.UnassignGatewayAccountGroupFromTeamAsync(_identity, new UnassignGatewayAccountGroupFromTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsGroupId = group.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain(group.Id, team.GatewayCredentialsGroupIds);
    }

    [Fact]
    public async Task AssignGatewayAccountToTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.AssignGatewayAccountToTeamAsync(_identity, default(AssignGatewayAccountToTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task AssignGatewayAccountToTeamAsync_ValidRequest_AssignsCredential()
    {
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.Manual);
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterGatewayCredential("cred-1");

        var result = await sut.AssignGatewayAccountToTeamAsync(_identity, new AssignGatewayAccountToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.Contains("cred-1", team.GatewayCredentialsIds);
    }

    [Fact]
    public async Task UnassignGatewayAccountFromTeamAsync_NullRequest_ReturnsTeamIdInvalid()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.UnassignGatewayAccountFromTeamAsync(_identity, default(UnassignGatewayAccountFromTeamRequest));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task UnassignGatewayAccountFromTeamAsync_ValidRequest_UnassignsCredential()
    {
        var (teamId, _) = await SeedTeamAsync(strategy: GatewaySelectionStrategy.Manual);
        var sut = _ctx.CreateOperationAdministrator();
        _ctx.RegisterGatewayCredential("cred-1");
        await sut.AssignGatewayAccountToTeamAsync(_identity, new AssignGatewayAccountToTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        var result = await sut.UnassignGatewayAccountFromTeamAsync(_identity, new UnassignGatewayAccountFromTeamRequest
        {
            TeamId = teamId,
            GatewayCredentialsId = "cred-1"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var team = _ctx.Teams.AsQueryable().First(t => t.Id == teamId);
        Assert.DoesNotContain("cred-1", team.GatewayCredentialsIds);
    }

    private async Task<(string TeamId, string OperationId)> SeedTeamAsync(
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman)
    {
        var operation = await _ctx.SeedOperationAsync(
            "Team Parent Operation",
            administratorIds: new[] { "op-admin-1" });
        var team = await _ctx.SeedTeamAsync(operation.Id, strategy: strategy);
        return (team.Id, operation.Id);
    }
}

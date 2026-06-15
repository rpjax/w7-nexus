using Nexus.OperationAdministrator.Application.Requests;
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
}

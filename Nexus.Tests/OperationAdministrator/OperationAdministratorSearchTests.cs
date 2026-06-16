using Nexus.OperationAdministrator.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.OperationAdministrator;

public sealed class OperationAdministratorSearchTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task SearchOperationsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.SearchOperationsAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchOperationsAsync_OpAdminSeesOnlyAssignedOperations()
    {
        await _ctx.SeedOperationAsync("Assigned Operation", administratorIds: new[] { "op-admin-1" });
        await _ctx.SeedOperationAsync("Other Operation", administratorIds: new[] { "other-admin" });
        var sut = _ctx.CreateOperationAdministrator("op-admin-1");

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Assigned Operation", result.Value.Items[0].Name);
        Assert.Equal(1, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_UnassignedOpAdmin_ReturnsEmptyList()
    {
        await _ctx.SeedOperationAsync("Other Operation", administratorIds: new[] { "other-admin" });
        var sut = _ctx.CreateOperationAdministrator("op-admin-1");

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_GlobalAdministratorSeesAllOperations()
    {
        await _ctx.SeedOperationAsync("Op A", administratorIds: new[] { "op-admin-1" });
        await _ctx.SeedOperationAsync("Op B", administratorIds: new[] { "other-admin" });
        var sut = _ctx.CreateOperationAdministrator("global-admin", isGlobalAdministrator: true);

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(2, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordFilter_FiltersAssignedOperations()
    {
        await _ctx.SeedOperationAsync("Alpha Operation", administratorIds: new[] { "op-admin-1" });
        await _ctx.SeedOperationAsync("Beta Operation", administratorIds: new[] { "op-admin-1" });
        var sut = _ctx.CreateOperationAdministrator("op-admin-1");

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = "alpha"
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Alpha Operation", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task SearchOperationsAsync_ReturnsEnrichedTeamsAndAdministrators()
    {
        var operation = await _ctx.SeedOperationAsync(
            "Enriched Operation",
            administratorIds: new[] { "op-admin-1" });
        await _ctx.SeedAccountAsync("opadmin", id: "op-admin-1");
        await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-1" });
        await _ctx.SeedAccountAsync("operator1", id: "operator-1");
        var sut = _ctx.CreateOperationAdministrator("op-admin-1");

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Single(item.Administrators);
        Assert.Equal("opadmin", item.Administrators[0].Username);
        Assert.Single(item.Teams);
        Assert.Single(item.Teams[0].Operators);
        Assert.Equal("operator1", item.Teams[0].Operators[0].Username);
    }

    [Fact]
    public async Task SearchOperationsAsync_InvalidLimit_ReturnsValidationError()
    {
        var sut = _ctx.CreateOperationAdministrator();

        var result = await sut.SearchOperationsAsync(new SearchOperationAdministratorOperationsRequest
        {
            Limit = 1000,
            Offset = 0
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchLimitInvalid);
    }
}

using Nexus.Operator.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Operator;

public sealed class OperatorTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task SearchOperationsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorNotAssignedToAnyTeam_ReturnsEmptyList()
    {
        await _ctx.SeedOperationAsync("Visible Operation");
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorAssignedToTeam_ReturnsOperation()
    {
        var operation = await _ctx.SeedOperationAsync("Assigned Operation", "desc");
        await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-1" });
        await _ctx.SeedOperationAsync("Other Operation");
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
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
    public async Task SearchOperationsAsync_KeywordFilter_FiltersAssignedOperations()
    {
        var alpha = await _ctx.SeedOperationAsync("Alpha Operation");
        var beta = await _ctx.SeedOperationAsync("Beta Operation");
        await _ctx.SeedTeamAsync(alpha.Id, operatorIds: new[] { "operator-1" });
        await _ctx.SeedTeamAsync(beta.Id, operatorIds: new[] { "operator-1" });
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = "alpha"
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(alpha.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitZero_UsesDefaultLimitOfTwenty()
    {
        var sut = _ctx.CreateOperator("operator-1");
        for (var i = 0; i < 25; i++)
        {
            var operation = await _ctx.SeedOperationAsync($"Operation {i:D2}");
            await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-1" });
        }

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
        {
            Limit = 0,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value!.Items.Count);
        Assert.Equal(25, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_InvalidLimit_ReturnsValidationError()
    {
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
        {
            Limit = 1000,
            Offset = 0
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchLimitInvalid);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordTooLong_ReturnsValidationError()
    {
        var sut = _ctx.CreateOperator("operator-1");

        var result = await sut.SearchOperationsAsync(new SearchOperatorOperationsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = new string('A', Operation.MaxNameLength + 1)
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchKeywordTooLong);
    }
}

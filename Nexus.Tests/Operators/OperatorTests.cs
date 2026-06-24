using Nexus.Operators.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;
using Nexus.Authorization;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;

namespace Nexus.Tests.Operators;

public sealed class OperatorTests
{
    private readonly ActorTestContext _ctx = new();

    private RequesterIdentity Identity(string accountId = "operator-1")
        => _ctx.CreateRequesterIdentity(accountId, additionalRoles: Roles.Operator);

    [Fact]
    public async Task SearchOperationsAsync_WithoutOperatorRole_ReturnsUnauthorized()
    {
        var sut = _ctx.CreateOperator();
        var identity = _ctx.CreateRequesterIdentity("regular-user");

        var result = await sut.SearchOperationsAsync(identity, new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperator);
    }

    [Fact]
    public async Task SearchOperationsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), default(SearchOperationsRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorNotAssignedToAnyTeam_ReturnsEmptyList()
    {
        await _ctx.SeedOperationAsync("Visible Operation");
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorAssignedToTeam_ReturnsOperation()
    {
        var operation = await _ctx.SeedOperationAsync("Assigned Operation", "desc");
        await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-1" });
        await _ctx.SeedOperationAsync("Other Operation");
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("Assigned Operation", result.Value.Items[0].Name);
        Assert.Equal("Test Team", result.Value.Items[0].Team.Name);
        Assert.Equal(1, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorInMultipleTeamsOfSameOperation_ReturnsOneItemPerTeam()
    {
        var operation = await _ctx.SeedOperationAsync("Shared Operation");
        await _ctx.SeedTeamAsync(operation.Id, name: "Team A", operatorIds: new[] { "operator-1" });
        await _ctx.SeedTeamAsync(operation.Id, name: "Team B", operatorIds: new[] { "operator-1" });
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.Total);
        Assert.All(result.Value.Items, item => Assert.Equal(operation.Id, item.Id));
        Assert.Contains(result.Value.Items, item => item.Team.Name == "Team A");
        Assert.Contains(result.Value.Items, item => item.Team.Name == "Team B");
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordFilter_FiltersAssignedOperations()
    {
        var alpha = await _ctx.SeedOperationAsync("Alpha Operation");
        var beta = await _ctx.SeedOperationAsync("Beta Operation");
        await _ctx.SeedTeamAsync(alpha.Id, operatorIds: new[] { "operator-1" });
        await _ctx.SeedTeamAsync(beta.Id, operatorIds: new[] { "operator-1" });
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = "alpha"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(alpha.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitZero_UsesDefaultLimitOfTwenty()
    {
        var sut = _ctx.CreateOperator();
        for (var i = 0; i < 25; i++)
        {
            var operation = await _ctx.SeedOperationAsync($"Operation {i:D2}");
            await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-1" });
        }

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 0,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Equal(25, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_InvalidLimit_ReturnsValidationError()
    {
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 1000,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchLimitInvalid);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordTooLong_ReturnsValidationError()
    {
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0,
            Keyword = new string('A', Operation.MaxNameLength + 1)
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchKeywordTooLong);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorMentionedInProfitShareCut_ReturnsEmptyList()
    {
        var operation = await _ctx.SeedOperationAsync("Profit Share Operation");
        var team = await _ctx.SeedTeamAsync(operation.Id, operatorIds: new[] { "operator-2" });
        _ctx.RegisterAccount("operator-1");
        _ctx.RegisterAccount("operator-2");

        var teamService = _ctx.CreateTeamService();
        var setRuleResult = await teamService.SetOperatorProfitShareRuleAsync(
            team.Id,
            "operator-2",
            new[] { new ProfitSplit("operator-1", 100m) });
        Assert.True(setRuleResult.IsSuccess);

        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_OperatorMentionedInPayment_ReturnsEmptyList()
    {
        var operation = await _ctx.SeedOperationAsync("Payment Operation");
        await _ctx.SeedPaymentAsync(operation.Id, operatorId: "operator-1");
        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_ShowsViewerProfitShareOnTeamOnly()
    {
        var operation = await _ctx.SeedOperationAsync("Profit Share Operation");
        var team = await _ctx.SeedTeamAsync(
            operation.Id,
            operatorIds: new[] { "operator-1", "operator-2" });
        await _ctx.SeedAccountAsync("viewer", id: "operator-1");
        await _ctx.SeedAccountAsync("other", id: "operator-2");
        _ctx.RegisterAccount("operator-1");
        _ctx.RegisterAccount("operator-2");

        var teamService = _ctx.CreateTeamService();
        Assert.True((await teamService.SetOperatorProfitShareRuleAsync(
            team.Id,
            "operator-1",
            new[] { new ProfitSplit("operator-1", 100m) })).IsSuccess);
        Assert.True((await teamService.SetOperatorProfitShareRuleAsync(
            team.Id,
            "operator-2",
            new[] { new ProfitSplit("operator-2", 100m) })).IsSuccess);

        var sut = _ctx.CreateOperator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(2, item.Team.Operators.Length);
        Assert.Contains(item.Team.Operators, o => o.AccountId == "operator-1" && o.Username == "viewer");
        Assert.Contains(item.Team.Operators, o => o.AccountId == "operator-2" && o.Username == "other");
        Assert.Single(item.Team.ProfitShareRule.Cuts);
        Assert.Equal(100m, item.Team.ProfitShareRule.Cuts[0].Percentage);
        Assert.Equal("operator-1", item.Team.ProfitShareRule.Cuts[0].AccountId);
    }
}

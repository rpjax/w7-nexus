using Nexus.Administrators.Application.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Accounts.Errors;
using Nexus.Tests.Support;
using Xunit;
using Nexus.Authorization;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;

namespace Nexus.Tests.Administrators;

public sealed class AdministratorTests
{
    private readonly ActorTestContext _ctx = new();

    private RequesterIdentity Identity(string accountId = "admin-1")
        => _ctx.CreateRequesterIdentity(accountId, additionalRoles: Roles.Administrator);

    [Fact]
    public async Task CreateOperationAsync_WithoutAdministratorRole_ReturnsUnauthorized()
    {
        var sut = _ctx.CreateAdministrator();
        var identity = _ctx.CreateRequesterIdentity("global-admin", isGlobalAdministrator: false);

        var result = await sut.CreateOperationAsync(identity, new CreateOperationRequest
        {
            Name = "Denied Operation",
            Description = "desc"
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotAdministrator);
    }

    [Fact]
    public async Task SearchOperationsAsync_WithoutAdministratorRole_ReturnsUnauthorized()
    {
        var sut = _ctx.CreateAdministrator();
        var identity = _ctx.CreateRequesterIdentity("regular-user");

        var result = await sut.SearchOperationsAsync(identity, new SearchOperationsRequest
        {
            Limit = 20,
            Offset = 0
        });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotAdministrator);
    }

    [Fact]
    public async Task CreateOperationAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.CreateOperationAsync(Identity(), default(CreateOperationRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task CreateOperationAsync_ValidRequest_ReturnsOperationDetails()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.CreateOperationAsync(Identity(), new CreateOperationRequest
        {
            Name = "Alpha Operation",
            Description = "Primary test operation"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Alpha Operation", result.Value.Name);
        Assert.Equal("Primary test operation", result.Value.Description);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Id));
    }

    [Fact]
    public async Task CreateOperationAsync_NameTooLong_PropagatesServiceError()
    {
        var sut = _ctx.CreateAdministrator();
        var tooLongName = new string('A', Operation.MaxNameLength + 1);

        var result = await sut.CreateOperationAsync(Identity(), new CreateOperationRequest
        {
            Name = tooLongName,
            Description = "desc"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task SearchOperationsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchOperationsAsync(Identity(), default(SearchOperationsRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitZero_UsesDefaultLimitOfTwenty()
    {
        var sut = _ctx.CreateAdministrator();
        for (var i = 0; i < 25; i++)
            await _ctx.SeedOperationAsync($"Operation {i:D2}");

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 0,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(20, result.Value.Limit);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Equal(25, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitAtOrAboveMaximum_ReturnsSearchLimitInvalid()
    {
        var sut = _ctx.CreateAdministrator();

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
    public async Task SearchOperationsAsync_NegativeOffset_ReturnsSearchOffsetInvalid()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Offset = -1
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchOffsetInvalid);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordTooLong_ReturnsSearchKeywordTooLong()
    {
        var sut = _ctx.CreateAdministrator();
        var tooLongKeyword = new string('k', Operation.MaxNameLength + 1);

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = tooLongKeyword
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchKeywordTooLong);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordFilter_MatchesNameDescriptionAndId()
    {
        var sut = _ctx.CreateAdministrator();
        var target = await _ctx.SeedOperationAsync("UniqueAlpha", "beta description");
        await _ctx.SeedOperationAsync("Other Operation", "unrelated");

        var byName = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = "uniquealpha"
        });
        Assert.True(byName.IsAuthorized);
        Assert.True(byName.IsSuccess);
        Assert.NotNull(byName.Value);
        Assert.Single(byName.Value.Items);
        Assert.Equal(target.Id, byName.Value.Items[0].Id);

        var byDescription = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = "beta desc"
        });
        Assert.True(byDescription.IsAuthorized);
        Assert.True(byDescription.IsSuccess);
        Assert.NotNull(byDescription.Value);
        Assert.Single(byDescription.Value.Items);
        Assert.Equal(target.Id, byDescription.Value.Items[0].Id);

        var byId = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = target.Id[..8]
        });
        Assert.True(byId.IsAuthorized);
        Assert.True(byId.IsSuccess);
        Assert.NotNull(byId.Value);
        Assert.Contains(byId.Value.Items, i => i.Id == target.Id);
    }

    [Fact]
    public async Task SearchOperationsAsync_AdministratorIdsFilter_ReturnsMatchingOperations()
    {
        var sut = _ctx.CreateAdministrator();
        await _ctx.SeedOperationAsync("Op A", administratorIds: ["admin-1", "admin-2"]);
        await _ctx.SeedOperationAsync("Op B", administratorIds: ["admin-3"]);
        await _ctx.SeedOperationAsync("Op C", administratorIds: ["admin-1"]);

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 50,
            AdministratorIds = [" admin-1 ", "admin-1"]
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Total);
        var names = result.Value.Items.Select(i => i.Name).ToArray();
        Assert.Contains("Op A", names);
        Assert.Contains("Op C", names);
        Assert.DoesNotContain("Op B", names);
    }

    [Fact]
    public async Task SearchOperationsAsync_Pagination_ReturnsCorrectPage()
    {
        var sut = _ctx.CreateAdministrator();
        for (var i = 0; i < 5; i++)
            await _ctx.SeedOperationAsync($"Paged Op {i}");

        var page = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 2,
            Offset = 2
        });

        Assert.True(page.IsAuthorized);
        Assert.True(page.IsSuccess);
        Assert.NotNull(page.Value);
        Assert.Equal(2, page.Value.Limit);
        Assert.Equal(2, page.Value.Offset);
        Assert.Equal(5, page.Value.Total);
        Assert.Equal(2, page.Value.Items.Count);
    }

    [Fact]
    public async Task SearchOperationsAsync_ReturnsEnrichedAdministratorsAndTeams()
    {
        var sut = _ctx.CreateAdministrator();
        var admin = await _ctx.SeedAccountAsync("globaladmin", id: "admin-1", roles: ["administrator"]);
        var leader = await _ctx.SeedAccountAsync("teamleader", id: "leader-1");
        var operatorAccount = await _ctx.SeedAccountAsync("operator1", id: "operator-1");
        var operation = await _ctx.SeedOperationAsync("Enriched Op", administratorIds: [admin.Id]);
        var team = await _ctx.SeedTeamAsync(
            operation.Id,
            name: "Alpha Team",
            operatorIds: [operatorAccount.Id]);
        _ctx.RegisterAccount(leader.Id);
        await _ctx.CreateTeamService().AssignTeamLeaderAsync(team.Id, leader.Id);

        var result = await sut.SearchOperationsAsync(Identity(), new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = "enriched"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Enriched Op", item.Name);

        var mappedAdmin = Assert.Single(item.Administrators);
        Assert.Equal(admin.Id, mappedAdmin.AccountId);
        Assert.Equal("globaladmin", mappedAdmin.Username);

        var mappedTeam = Assert.Single(item.Teams);
        Assert.Equal("Alpha Team", mappedTeam.Name);
        Assert.NotNull(mappedTeam.TeamLeader);
        Assert.Equal(leader.Id, mappedTeam.TeamLeader!.AccountId);
        Assert.Equal("teamleader", mappedTeam.TeamLeader.Username);

        var mappedOperator = Assert.Single(mappedTeam.Operators);
        Assert.Equal(operatorAccount.Id, mappedOperator.AccountId);
        Assert.Equal("operator1", mappedOperator.Username);
        Assert.Single(mappedOperator.ProfitShareRule.Cuts);
    }

    [Fact]
    public async Task DeleteOperationAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.DeleteOperationAsync(Identity(), default(DeleteOperationRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task DeleteOperationAsync_ValidRequest_DeletesOperation()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("To Delete");

        var result = await sut.DeleteOperationAsync(Identity(), new DeleteOperationRequest
        {
            OperationId = operation.Id
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(_ctx.Operations.AsQueryable().ToArray());
    }

    [Fact]
    public async Task DeleteOperationAsync_NotFound_PropagatesError()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.DeleteOperationAsync(Identity(), new DeleteOperationRequest
        {
            OperationId = "missing-op"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task AssignOperationAdministratorAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.AssignOperationAdministratorAsync(Identity(), default(AssignOperationAdministratorRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignOperationAdministratorAsync_ValidRequest_Succeeds()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op");
        _ctx.RegisterAccount("admin-42");

        var result = await sut.AssignOperationAdministratorAsync(Identity(), new AssignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var updated = _ctx.Operations.AsQueryable().First(o => o.Id == operation.Id);
        Assert.Contains("admin-42", updated.AdministratorIds);
    }

    [Fact]
    public async Task AssignOperationAdministratorAsync_AlreadyAssigned_PropagatesError()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op", administratorIds: ["admin-42"]);
        _ctx.RegisterAccount("admin-42");

        var result = await sut.AssignOperationAdministratorAsync(Identity(), new AssignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorAlreadyAssigned);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.UnassignOperationAdministratorAsync(Identity(), default(UnassignOperationAdministratorRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_ValidRequest_Succeeds()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op", administratorIds: ["admin-42"]);

        var result = await sut.UnassignOperationAdministratorAsync(Identity(), new UnassignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        var updated = _ctx.Operations.AsQueryable().First(o => o.Id == operation.Id);
        Assert.DoesNotContain("admin-42", updated.AdministratorIds);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_NotAssigned_PropagatesError()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op");

        var result = await sut.UnassignOperationAdministratorAsync(Identity(), new UnassignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorNotAssigned);
    }

    [Fact]
    public async Task SearchAccountsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchAccountsAsync(Identity(), default(SearchAccountsRequest));

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchAccountsAsync_LimitZero_UsesDefaultLimitOfTwenty()
    {
        var sut = _ctx.CreateAdministrator();
        for (var i = 0; i < 25; i++)
            await _ctx.SeedAccountAsync($"user{i:D2}");

        var result = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 0,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(20, result.Value.Limit);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Equal(25, result.Value.Total);
    }

    [Fact]
    public async Task SearchAccountsAsync_LimitAtOrAboveMaximum_ReturnsSearchLimitInvalid()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 1000,
            Offset = 0
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.SearchLimitInvalid);
    }

    [Fact]
    public async Task SearchAccountsAsync_NegativeOffset_ReturnsSearchOffsetInvalid()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 10,
            Offset = -1
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.SearchOffsetInvalid);
    }

    [Fact]
    public async Task SearchAccountsAsync_KeywordTooLong_ReturnsSearchKeywordTooLong()
    {
        var sut = _ctx.CreateAdministrator();
        var tooLongKeyword = new string('k', 201);

        var result = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 10,
            Keyword = tooLongKeyword
        });

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.SearchKeywordTooLong);
    }

    [Fact]
    public async Task SearchAccountsAsync_KeywordFilter_MatchesUsernameAndId()
    {
        var sut = _ctx.CreateAdministrator();
        var target = await _ctx.SeedAccountAsync("UniqueAlpha");
        await _ctx.SeedAccountAsync("OtherUser");

        var byUsername = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 10,
            Keyword = "uniquealpha"
        });
        Assert.True(byUsername.IsAuthorized);
        Assert.True(byUsername.IsSuccess);
        Assert.NotNull(byUsername.Value);
        Assert.Single(byUsername.Value.Items);
        Assert.Equal(target.Id, byUsername.Value.Items[0].Id);

        var byId = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 10,
            Keyword = target.Id[..8]
        });
        Assert.True(byId.IsAuthorized);
        Assert.True(byId.IsSuccess);
        Assert.NotNull(byId.Value);
        Assert.Contains(byId.Value.Items, i => i.Id == target.Id);
    }

    [Fact]
    public async Task SearchAccountsAsync_Pagination_ReturnsCorrectPage()
    {
        var sut = _ctx.CreateAdministrator();
        for (var i = 0; i < 5; i++)
            await _ctx.SeedAccountAsync($"PagedUser{i}");

        var page = await sut.SearchAccountsAsync(Identity(), new SearchAccountsRequest
        {
            Limit = 2,
            Offset = 2
        });

        Assert.True(page.IsAuthorized);
        Assert.True(page.IsSuccess);
        Assert.NotNull(page.Value);
        Assert.Equal(2, page.Value.Limit);
        Assert.Equal(2, page.Value.Offset);
        Assert.Equal(5, page.Value.Total);
        Assert.Equal(2, page.Value.Items.Count);
    }
}

using Nexus.Actors.Requests;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Actors;

public sealed class AdministratorTests
{
    private readonly ActorTestContext _ctx = new();

    [Fact]
    public async Task CreateOperationAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.CreateOperationAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task CreateOperationAsync_ValidRequest_ReturnsOperationDetails()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.CreateOperationAsync(new CreateOperationRequest
        {
            Name = "Alpha Operation",
            Description = "Primary test operation"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Alpha Operation", result.Value!.Name);
        Assert.Equal("Primary test operation", result.Value.Description);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Id));
    }

    [Fact]
    public async Task CreateOperationAsync_NameTooLong_PropagatesServiceError()
    {
        var sut = _ctx.CreateAdministrator();
        var tooLongName = new string('A', Operation.MaxNameLength + 1);

        var result = await sut.CreateOperationAsync(new CreateOperationRequest
        {
            Name = tooLongName,
            Description = "desc"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task SearchOperationsAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchOperationsAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitZero_UsesDefaultLimitOfTwenty()
    {
        var sut = _ctx.CreateAdministrator();
        for (var i = 0; i < 25; i++)
            await _ctx.SeedOperationAsync($"Operation {i:D2}");

        var result = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 0,
            Offset = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value!.Limit);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.Equal(25, result.Value.Total);
    }

    [Fact]
    public async Task SearchOperationsAsync_LimitAtOrAboveMaximum_ReturnsSearchLimitInvalid()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 1000,
            Offset = 0
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchLimitInvalid);
    }

    [Fact]
    public async Task SearchOperationsAsync_NegativeOffset_ReturnsSearchOffsetInvalid()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 10,
            Offset = -1
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchOffsetInvalid);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordTooLong_ReturnsSearchKeywordTooLong()
    {
        var sut = _ctx.CreateAdministrator();
        var tooLongKeyword = new string('k', Operation.MaxNameLength + 1);

        var result = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = tooLongKeyword
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.SearchKeywordTooLong);
    }

    [Fact]
    public async Task SearchOperationsAsync_KeywordFilter_MatchesNameDescriptionAndId()
    {
        var sut = _ctx.CreateAdministrator();
        var target = await _ctx.SeedOperationAsync("UniqueAlpha", "beta description");
        await _ctx.SeedOperationAsync("Other Operation", "unrelated");

        var byName = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = "uniquealpha"
        });
        Assert.True(byName.IsSuccess);
        Assert.Single(byName.Value!.Items);
        Assert.Equal(target.Id, byName.Value.Items[0].Id);

        var byDescription = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = "beta desc"
        });
        Assert.True(byDescription.IsSuccess);
        Assert.Single(byDescription.Value!.Items);
        Assert.Equal(target.Id, byDescription.Value.Items[0].Id);

        var byId = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 10,
            Keyword = target.Id[..8]
        });
        Assert.True(byId.IsSuccess);
        Assert.Contains(byId.Value!.Items, i => i.Id == target.Id);
    }

    [Fact]
    public async Task SearchOperationsAsync_AdministratorIdsFilter_ReturnsMatchingOperations()
    {
        var sut = _ctx.CreateAdministrator();
        await _ctx.SeedOperationAsync("Op A", administratorIds: ["admin-1", "admin-2"]);
        await _ctx.SeedOperationAsync("Op B", administratorIds: ["admin-3"]);
        await _ctx.SeedOperationAsync("Op C", administratorIds: ["admin-1"]);

        var result = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 50,
            AdministratorIds = [" admin-1 ", "admin-1"]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Total);
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

        var page = await sut.SearchOperationsAsync(new SearchOperationsRequest
        {
            Limit = 2,
            Offset = 2
        });

        Assert.True(page.IsSuccess);
        Assert.Equal(2, page.Value!.Limit);
        Assert.Equal(2, page.Value.Offset);
        Assert.Equal(5, page.Value.Total);
        Assert.Equal(2, page.Value.Items.Count);
    }

    [Fact]
    public async Task DeleteOperationAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.DeleteOperationAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task DeleteOperationAsync_ValidRequest_DeletesOperation()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("To Delete");

        var result = await sut.DeleteOperationAsync(new DeleteOperationRequest
        {
            OperationId = operation.Id
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(_ctx.Operations.AsQueryable().ToArray());
    }

    [Fact]
    public async Task DeleteOperationAsync_NotFound_PropagatesError()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.DeleteOperationAsync(new DeleteOperationRequest
        {
            OperationId = "missing-op"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task AssignOperationAdministratorAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.AssignOperationAdministratorAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task AssignOperationAdministratorAsync_ValidRequest_Succeeds()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op");
        _ctx.RegisterAccount("admin-42");

        var result = await sut.AssignOperationAdministratorAsync(new AssignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

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

        var result = await sut.AssignOperationAdministratorAsync(new AssignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorAlreadyAssigned);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_NullRequest_ReturnsRequestBodyRequired()
    {
        var sut = _ctx.CreateAdministrator();

        var result = await sut.UnassignOperationAdministratorAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.RequestBodyRequired);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_ValidRequest_Succeeds()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op", administratorIds: ["admin-42"]);

        var result = await sut.UnassignOperationAdministratorAsync(new UnassignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsSuccess);
        var updated = _ctx.Operations.AsQueryable().First(o => o.Id == operation.Id);
        Assert.DoesNotContain("admin-42", updated.AdministratorIds);
    }

    [Fact]
    public async Task UnassignOperationAdministratorAsync_NotAssigned_PropagatesError()
    {
        var sut = _ctx.CreateAdministrator();
        var operation = await _ctx.SeedOperationAsync("Managed Op");

        var result = await sut.UnassignOperationAdministratorAsync(new UnassignOperationAdministratorRequest
        {
            OperationId = operation.Id,
            AdministratorId = "admin-42"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorNotAssigned);
    }
}

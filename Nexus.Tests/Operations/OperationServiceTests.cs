using System.Linq.Expressions;
using Nexus.Gateways.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Gateways.Aggregates;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Tests.Payments;
using Nexus.Tests.Support;
using Xunit;
using Nexus.Operations.Errors;

namespace Nexus.Tests.Operations;

public sealed class OperationServiceTests
{
    private const string OperationId = "op-1";
    private const string GroupId = "group-1";
    private const string CredentialId = "cred-1";
    private const string StrawManId = "straw-1";

    private sealed class InMemoryOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _store = new();

        public IAsyncQueryable<Operation> AsQueryable()
            => new MongoAsyncQueryable<Operation>(_store.AsQueryable());

        public Task<Operation> CreateAsync(Operation entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? new Operation(
                    Guid.NewGuid().ToString("N"),
                    entity.Name,
                    entity.Description,
                    entity.AdministratorIds,
                    entity.StrawManIds,
                    entity.GatewaySelectionStrategy,
                    entity.GatewayCredentialsIds,
                    entity.GatewayCredentialsGroupIds,
                    entity.CreatedAt,
                    entity.UpdatedAt)
                : entity;

            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Operation>.CreateAsync(Operation entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Operation> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Operation entity)
        {
            _store.RemoveAll(x => x.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(x => compiled(x));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Operation entity)
        {
            var index = _store.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class TestContext
    {
        public InMemoryOperationRepository Operations { get; } = new();
        public InMemoryGatewayCredentialsGroupRepository GatewayGroups { get; } = new();
        public FakeAccountIdValidator AccountValidator { get; } = new();
        public FakeGatewayCredentialsIdValidator GatewayCredentialsValidator { get; } = new();

        public OperationService CreateSut()
            => new(
                Operations,
                AccountValidator,
                GatewayGroups,
                GatewayCredentialsValidator);
    }

    private static TestContext CreateContextWithOperation(string operationId = OperationId)
    {
        var ctx = new TestContext();
        var operation = new Operation(
            operationId,
            "Test Operation",
            "Description",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
        ctx.Operations.CreateAsync(operation).GetAwaiter().GetResult();
        return ctx;
    }

    [Fact]
    public async Task CreateOperationAsync_DescriptionMissing_AllowsNullDescription()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.CreateOperationAsync(
            name: "Operation A",
            description: null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value!.Description);
        Assert.Equal(GatewaySelectionStrategy.PerStrawman, result.Value.GatewaySelectionStrategy);
    }

    [Fact]
    public async Task CreateOperationAsync_NameTooLong_ReturnsError()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var tooLongName = new string('A', Operation.MaxNameLength + 1);

        var result = await sut.CreateOperationAsync(
            name: tooLongName,
            description: "desc");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task CreateOperationAsync_NameAlreadyExists_IgnoresCaseAndSpaces()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var first = await sut.CreateOperationAsync(
            name: "My Operation",
            description: "x");
        Assert.True(first.IsSuccess);

        var duplicate = await sut.CreateOperationAsync(
            name: "  my operation  ",
            description: "y");

        Assert.True(duplicate.IsFailure);
        Assert.Contains(duplicate.Errors, e => e.Code == OperationErrorCodes.NameAlreadyExists);
    }

    [Fact]
    public async Task CreateOperationAsync_EmptyName_ReturnsNameInvalid()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.CreateOperationAsync(
            name: "   ",
            description: "desc");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.NameInvalid);
    }

    [Fact]
    public async Task CreateOperationAsync_DescriptionTooLong_ReturnsError()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var tooLongDescription = new string('D', Operation.MaxDescriptionLength + 1);

        var result = await sut.CreateOperationAsync(
            name: "Operation A",
            description: tooLongDescription);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.DescriptionTooLong);
    }

    [Fact]
    public async Task AssignAdministratorAsync_ValidIds_ReturnsSuccess()
    {
        var ctx = new TestContext();
        ctx.AccountValidator.AddExisting("admin-1");
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignAdministratorAsync(created.Value!.Id, "admin-1");

        Assert.True(result.IsSuccess);
        var operation = ctx.Operations.AsQueryable().First();
        Assert.Contains("admin-1", operation.AdministratorIds);
    }

    [Fact]
    public async Task AssignAdministratorAsync_AdminNotFound_ReturnsAdministratorAccountNotFound()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignAdministratorAsync(created.Value!.Id, "missing-admin");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorAccountNotFound);
    }

    [Fact]
    public async Task AssignAdministratorAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var ctx = new TestContext();
        ctx.AccountValidator.AddExisting("admin-1");
        var sut = ctx.CreateSut();

        var result = await sut.AssignAdministratorAsync("missing-op", "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task AssignAdministratorAsync_Duplicate_ReturnsAdministratorAlreadyAssigned()
    {
        var ctx = new TestContext();
        ctx.AccountValidator.AddExisting("admin-1");
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var first = await sut.AssignAdministratorAsync(created.Value!.Id, "admin-1");
        Assert.True(first.IsSuccess);

        var duplicate = await sut.AssignAdministratorAsync(created.Value.Id, "admin-1");

        Assert.True(duplicate.IsFailure);
        Assert.Contains(duplicate.Errors, e => e.Code == OperationErrorCodes.AdministratorAlreadyAssigned);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_AssignedAdministrator_ReturnsSuccess()
    {
        var ctx = new TestContext();
        ctx.AccountValidator.AddExisting("admin-1");
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);
        await sut.AssignAdministratorAsync(created.Value!.Id, "admin-1");

        var result = await sut.UnassignAdministratorAsync(created.Value.Id, "admin-1");

        Assert.True(result.IsSuccess);
        var operation = ctx.Operations.AsQueryable().First();
        Assert.DoesNotContain("admin-1", operation.AdministratorIds);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.UnassignAdministratorAsync("missing-op", "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_AdministratorNotAssigned_ReturnsAdministratorNotAssigned()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignAdministratorAsync(created.Value!.Id, "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorNotAssigned);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_InvalidAdministratorId_ReturnsAdministratorInvalid()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignAdministratorAsync(created.Value!.Id, "  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorInvalid);
    }

    [Fact]
    public async Task DeleteOperationAsync_ExistingOperation_ReturnsSuccess()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.DeleteOperationAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(ctx.Operations.AsQueryable().ToList());
    }

    [Fact]
    public async Task DeleteOperationAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.DeleteOperationAsync("missing-op");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task DeleteOperationAsync_InvalidOperationId_ReturnsOperationIdInvalid()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.DeleteOperationAsync("  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task SetGatewaySelectionStrategyAsync_ValidStrategy_UpdatesStrategy()
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.SetGatewaySelectionStrategyAsync(OperationId, GatewaySelectionStrategy.Manual);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            GatewaySelectionStrategy.Manual,
            ctx.Operations.AsQueryable().First().GatewaySelectionStrategy);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_WhenPerGroupStrategy_AssignsGroup()
    {
        var ctx = CreateContextWithOperation();
        await ctx.GatewayGroups.CreateAsync(new GatewayCredentialsGroup(
            GroupId,
            "Group A",
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
        var sut = ctx.CreateSut();
        await sut.SetGatewaySelectionStrategyAsync(OperationId, GatewaySelectionStrategy.PerGroup);

        var result = await sut.AssignGatewayCredentialsGroupAsync(OperationId, GroupId);

        Assert.True(result.IsSuccess);
        Assert.Contains(GroupId, ctx.Operations.AsQueryable().First().GatewayCredentialsGroupIds);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_WhenWrongStrategy_ReturnsStrategyMismatch()
    {
        var ctx = CreateContextWithOperation();
        await ctx.GatewayGroups.CreateAsync(new GatewayCredentialsGroup(
            GroupId,
            "Group A",
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(OperationId, GroupId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.GatewayCredentialsGroupStrategyMismatch);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_WhenManualStrategy_AssignsCredential()
    {
        var ctx = CreateContextWithOperation();
        ctx.GatewayCredentialsValidator.AddExisting(CredentialId);
        var sut = ctx.CreateSut();
        await sut.SetGatewaySelectionStrategyAsync(OperationId, GatewaySelectionStrategy.Manual);

        var result = await sut.AssignGatewayCredentialsAsync(OperationId, CredentialId);

        Assert.True(result.IsSuccess);
        Assert.Contains(CredentialId, ctx.Operations.AsQueryable().First().GatewayCredentialsIds);
    }

    [Fact]
    public async Task AssignStrawManAsync_ValidAccount_AssignsStrawMan()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(StrawManId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignStrawManAsync(OperationId, StrawManId);

        Assert.True(result.IsSuccess);
        Assert.Contains(StrawManId, ctx.Operations.AsQueryable().First().StrawManIds);
    }
}

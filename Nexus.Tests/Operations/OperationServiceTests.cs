using System.Linq.Expressions;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Tests.Payments;
using Xunit;
using Nexus.Operations.Errors;

namespace Nexus.Tests.Operations;

public sealed class OperationServiceTests
{
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

    [Fact]
    public async Task CreateOperationAsync_DescriptionMissing_AllowsNullDescription()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());

        var result = await sut.CreateOperationAsync(
            name: "Operation A",
            description: null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value!.Description);
    }

    [Fact]
    public async Task CreateOperationAsync_NameTooLong_ReturnsError()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
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
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());

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
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());

        var result = await sut.CreateOperationAsync(
            name: "   ",
            description: "desc");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.NameInvalid);
    }

    [Fact]
    public async Task CreateOperationAsync_DescriptionTooLong_ReturnsError()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
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
        var repo = new InMemoryOperationRepository();
        var validator = new FakeAccountIdValidator(["admin-1"]);
        var sut = new OperationService(repo, validator);
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignAdministratorAsync(created.Value!.Id, "admin-1");

        Assert.True(result.IsSuccess);
        var operation = repo.AsQueryable().First();
        Assert.Contains("admin-1", operation.AdministratorIds);
    }

    [Fact]
    public async Task AssignAdministratorAsync_AdminNotFound_ReturnsAdministratorAccountNotFound()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignAdministratorAsync(created.Value!.Id, "missing-admin");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorAccountNotFound);
    }

    [Fact]
    public async Task AssignAdministratorAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var validator = new FakeAccountIdValidator(["admin-1"]);
        var sut = new OperationService(new InMemoryOperationRepository(), validator);

        var result = await sut.AssignAdministratorAsync("missing-op", "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task AssignAdministratorAsync_Duplicate_ReturnsAdministratorAlreadyAssigned()
    {
        var repo = new InMemoryOperationRepository();
        var validator = new FakeAccountIdValidator(["admin-1"]);
        var sut = new OperationService(repo, validator);
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
        var repo = new InMemoryOperationRepository();
        var validator = new FakeAccountIdValidator(["admin-1"]);
        var sut = new OperationService(repo, validator);
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);
        await sut.AssignAdministratorAsync(created.Value!.Id, "admin-1");

        var result = await sut.UnassignAdministratorAsync(created.Value.Id, "admin-1");

        Assert.True(result.IsSuccess);
        var operation = repo.AsQueryable().First();
        Assert.DoesNotContain("admin-1", operation.AdministratorIds);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var sut = new OperationService(new InMemoryOperationRepository(), new FakeAccountIdValidator());

        var result = await sut.UnassignAdministratorAsync("missing-op", "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_AdministratorNotAssigned_ReturnsAdministratorNotAssigned()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignAdministratorAsync(created.Value!.Id, "admin-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorNotAssigned);
    }

    [Fact]
    public async Task UnassignAdministratorAsync_InvalidAdministratorId_ReturnsAdministratorInvalid()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignAdministratorAsync(created.Value!.Id, "  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.AdministratorInvalid);
    }

    [Fact]
    public async Task DeleteOperationAsync_ExistingOperation_ReturnsSuccess()
    {
        var repo = new InMemoryOperationRepository();
        var sut = new OperationService(repo, new FakeAccountIdValidator());
        var created = await sut.CreateOperationAsync("Operation A", "desc");
        Assert.True(created.IsSuccess);

        var result = await sut.DeleteOperationAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.AsQueryable().ToList());
    }

    [Fact]
    public async Task DeleteOperationAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var sut = new OperationService(new InMemoryOperationRepository(), new FakeAccountIdValidator());

        var result = await sut.DeleteOperationAsync("missing-op");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task DeleteOperationAsync_InvalidOperationId_ReturnsOperationIdInvalid()
    {
        var sut = new OperationService(new InMemoryOperationRepository(), new FakeAccountIdValidator());

        var result = await sut.DeleteOperationAsync("  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == OperationErrorCodes.OperationIdInvalid);
    }
}

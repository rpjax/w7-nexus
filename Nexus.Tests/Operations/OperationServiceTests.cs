using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.ErrorCodes;
using Nexus.Operations.Infrastructure;
using Nexus.Tests.Payments;
using Xunit;

namespace Nexus.Tests.Operations;

public sealed class OperationServiceTests
{
    private sealed class InMemoryOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _store = new();

        public IAsyncQueryable<Operation> AsQueryable()
            => new MongoAsyncQueryable<Operation>(_store.AsQueryable());

        public Task CreateAsync(Operation entity)
        {
            _store.Add(entity);
            return Task.CompletedTask;
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
            description: null,
            operatorIds: Array.Empty<string>());

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
            description: "desc",
            operatorIds: Array.Empty<string>());

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
            description: "x",
            operatorIds: Array.Empty<string>());
        Assert.True(first.IsSuccess);

        var duplicate = await sut.CreateOperationAsync(
            name: "  my operation  ",
            description: "y",
            operatorIds: Array.Empty<string>());

        Assert.True(duplicate.IsFailure);
        Assert.Contains(duplicate.Errors, e => e.Code == OperationErrorCodes.NameAlreadyExists);
    }
}

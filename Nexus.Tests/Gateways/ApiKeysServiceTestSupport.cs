using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Services.Contracts;

namespace Nexus.Tests.Gateways;

internal static class ApiKeysServiceTestSupport
{
    internal static string Repeat(char c, int count) => new(c, count);

    internal static IMongoCollection<T> CreateThrowIfCalledMongoCollection<T>()
    {
        var collection = new Mock<IMongoCollection<T>>(MockBehavior.Strict);

        collection
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        collection
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<CountOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        return collection.Object;
    }

    internal sealed class AsyncInMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _store = new();

        public IAsyncQueryable<Account> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<Account>(_store.AsQueryable());

        public Task<Account> CreateAsync(Account entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? new Account(
                    Guid.NewGuid().ToString("N"),
                    entity.Username,
                    entity.PasswordHash,
                    entity.Roles,
                    entity.Permissions,
                    entity.CreatedAt,
                    entity.LastUpdatedAt)
                : entity;

            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Account>.CreateAsync(Account entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Account> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Account entity)
        {
            _store.RemoveAll(a => a.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(a => compiled(a));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Account entity)
        {
            var index = _store.FindIndex(a => a.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }
}

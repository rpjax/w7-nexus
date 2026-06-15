using System.Collections.Concurrent;
using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;

namespace Nexus.Tests.Accounts;

internal sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<string, Account> _store = new();

    public IAsyncQueryable<Account> AsQueryable()
    {
        var source = _store.Values.AsQueryable();
        return new QueryableToAsyncQueryableAdapter<Account>(source);
    }

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
            : CloneAccount(entity);

        _store[persisted.Id] = CloneAccount(persisted);
        return Task.FromResult(CloneAccount(persisted));
    }

    async Task IRepository<Account>.CreateAsync(Account entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<Account> entities)
    {
        foreach (var entity in entities)
            _store[entity.Id] = CloneAccount(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account entity)
    {
        _store.TryRemove(entity.Id, out _);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate)
    {
        var toRemove = _store.Values.Where(predicate.Compile()).ToList();
        var count = 0L;
        foreach (var account in toRemove)
        {
            if (_store.TryRemove(account.Id, out _))
                count++;
        }
        return Task.FromResult(count);
    }

    public Task UpdateAsync(Account entity)
    {
        _store[entity.Id] = CloneAccount(entity);
        return Task.CompletedTask;
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Account) instead.");
    }

    private static Account CloneAccount(Account a) =>
        new(a.Id, a.Username, a.PasswordHash, a.Roles, a.Permissions, a.CreatedAt, a.LastUpdatedAt);
}

using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.Accounts.Infrastructure.Persistance;

public sealed class MongoAccountRepository : IAccountRepository
{
    private readonly IMongoCollection<AccountRecord> _collection;

    public MongoAccountRepository(IMongoCollection<AccountRecord> collection)
    {
        _collection = collection;
    }

    private static readonly Expression<Func<AccountRecord, Account>> ToAccountProjection = r =>
        new Account(
            r.Id.ToString(),
            r.Username,
            r.PasswordHash,
            r.Roles,
            r.Permissions,
            r.CreatedAt,
            r.LastUpdatedAt);

    public IAsyncQueryable<Account> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToAccountProjection);

        return new MongoAsyncQueryable<Account>(source);
    }

    public async Task<Account> CreateAsync(Account entity)
    {
        var record = AccountRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return AccountRecordMapping.ToAccount(record);
    }

    async Task IRepository<Account>.CreateAsync(Account entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<Account> entities)
    {
        var records = entities.Select(AccountRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Account entity)
    {
        if (!ObjectId.TryParse(entity.Id, out var objectId))
            return Task.CompletedTask;

        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate)
    {
        var accountsToDelete = AsQueryable().Where(predicate).ToList();
        if (accountsToDelete.Count == 0)
            return 0;

        var ids = accountsToDelete
            .Select(a => ObjectId.TryParse(a.Id, out var oid) ? oid : ObjectId.Empty)
            .Where(oid => oid != ObjectId.Empty)
            .ToList();

        if (ids.Count == 0)
            return 0;

        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.Id));
        return result.DeletedCount;
    }

    public Task UpdateAsync(Account entity)
    {
        var record = AccountRecordMapping.ToRecord(entity);
        return _collection.ReplaceOneAsync(r => r.Id == record.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Account) instead.");
    }
}

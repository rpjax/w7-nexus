using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.BankAccounts.Infrastructure.Persistance;

public sealed class MongoBankAccountRepository : IBankAccountRepository
{
    private readonly IMongoCollection<BankAccountRecord> _collection;

    private static readonly Expression<Func<BankAccountRecord, BankAccount>> ToBankAccountProjection = r =>
        new BankAccount(
            r.Id.ToString(),
            r.OwnerId,
            r.Bank,
            r.Agency,
            r.AccountNumber,
            r.AccountDigit,
            r.AccountType,
            r.Label,
            r.CreatedAt,
            r.UpdatedAt,
            r.Balances.Select(b => new BankBalance(
                b.Id,
                b.AmountBrl,
                b.TransferId,
                b.CreatedAt,
                b.Splits.Select(s => new BankBalanceSplit(
                    s.AccountId,
                    s.Percentage,
                    s.Amount,
                    s.SplitKind)).ToList(),
                b.AppliedStrawManFeeIds,
                new BankBalanceOrigin(
                    b.Origin.OperationId,
                    b.Origin.OperatorId,
                    b.Origin.StrawManId))).ToList());

    public MongoBankAccountRepository(IMongoCollection<BankAccountRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<BankAccount> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToBankAccountProjection);
        return new MongoAsyncQueryable<BankAccount>(source);
    }

    public async Task<BankAccount> CreateAsync(BankAccount entity)
    {
        var record = BankAccountRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return BankAccountRecordMapping.ToBankAccount(record);
    }

    async Task IRepository<BankAccount>.CreateAsync(BankAccount entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<BankAccount> entities)
    {
        var records = entities.Select(BankAccountRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(BankAccount entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<BankAccount, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(a => ObjectId.Parse(a.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(BankAccount entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var record = BankAccountRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(BankAccount) instead.");
}

using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.AccountNodes.Infrastructure.Persistance;

public sealed class MongoBankAccountRepository : IBankAccountRepository
{
    private readonly IMongoCollection<AccountNodeBankAccountRecord> _collection;

    private static readonly Expression<Func<AccountNodeBankAccountRecord, BankAccount>> ToBankAccountProjection = r =>
        new BankAccount(
            r.Id.ToString(),
            r.StrawManId,
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
                b.SplitSnapshot.Select(s => new BalanceSplitSnapshot(
                    s.AccountId,
                    s.Percentage,
                    s.Amount,
                    s.SplitKind)).ToList(),
                b.AppliedStrawManFeeIds,
                new BalanceOriginSnapshot(
                    b.OriginSnapshot.OperationId,
                    b.OriginSnapshot.OperatorId,
                    b.OriginSnapshot.StrawManId))).ToList());

    public MongoBankAccountRepository(IMongoCollection<AccountNodeBankAccountRecord> collection)
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
        var record = AccountNodeRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return AccountNodeRecordMapping.ToBankAccount(record);
    }

    async Task IRepository<BankAccount>.CreateAsync(BankAccount entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<BankAccount> entities)
    {
        var records = entities.Select(AccountNodeRecordMapping.ToRecord);
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
        var record = AccountNodeRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(BankAccount) instead.");
}

public sealed class MongoCryptoWalletRepository : ICryptoWalletRepository
{
    private readonly IMongoCollection<AccountNodeCryptoWalletRecord> _collection;

    private static readonly Expression<Func<AccountNodeCryptoWalletRecord, CryptoWallet>> ToCryptoWalletProjection = r =>
        new CryptoWallet(
            r.Id.ToString(),
            r.StrawManId,
            r.Label,
            r.CreatedAt,
            r.UpdatedAt,
            r.Addresses.Select(a => new CryptoWalletAddress(a.Namespace, a.Address, a.Memo)).ToList(),
            r.Balances.Select(b => new CryptoBalance(
                b.Id,
                b.Chain,
                b.Asset,
                b.Amount,
                b.TransferId,
                b.CreatedAt,
                b.SplitSnapshot.Select(s => new BalanceSplitSnapshot(
                    s.AccountId,
                    s.Percentage,
                    s.Amount,
                    s.SplitKind)).ToList(),
                b.AppliedStrawManFeeIds,
                new BalanceOriginSnapshot(
                    b.OriginSnapshot.OperationId,
                    b.OriginSnapshot.OperatorId,
                    b.OriginSnapshot.StrawManId))).ToList());

    public MongoCryptoWalletRepository(IMongoCollection<AccountNodeCryptoWalletRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<CryptoWallet> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToCryptoWalletProjection);
        return new MongoAsyncQueryable<CryptoWallet>(source);
    }

    public async Task<CryptoWallet> CreateAsync(CryptoWallet entity)
    {
        var record = AccountNodeRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return AccountNodeRecordMapping.ToCryptoWallet(record);
    }

    async Task IRepository<CryptoWallet>.CreateAsync(CryptoWallet entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<CryptoWallet> entities)
    {
        var records = entities.Select(AccountNodeRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(CryptoWallet entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<CryptoWallet, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(w => ObjectId.Parse(w.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(CryptoWallet entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var record = AccountNodeRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(CryptoWallet) instead.");
}

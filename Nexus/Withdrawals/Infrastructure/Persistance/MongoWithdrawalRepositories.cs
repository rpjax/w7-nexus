using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Infrastructure.Mapping;

namespace Nexus.Withdrawals.Infrastructure.Persistance;

public sealed class MongoBankAccountRepository : IBankAccountRepository
{
    private readonly IMongoCollection<BankAccountRecord> _collection;

    private static readonly Expression<Func<BankAccountRecord, BankAccount>> ToProjection = r =>
        new BankAccount(
            r.Id.ToString(),
            r.StrawManAccountId,
            r.Bank,
            r.Agency,
            r.AccountNumber,
            r.AccountDigit,
            r.AccountType,
            r.PixKeyType,
            r.PixKey,
            r.Label,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoBankAccountRepository(IMongoCollection<BankAccountRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<BankAccount> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
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

public sealed class MongoCryptoWalletRepository : ICryptoWalletRepository
{
    private readonly IMongoCollection<CryptoWalletRecord> _collection;

    private static readonly Expression<Func<CryptoWalletRecord, CryptoWallet>> ToProjection = r =>
        new CryptoWallet(
            r.Id.ToString(),
            r.StrawManAccountId,
            r.Chain,
            r.Asset,
            r.Address,
            r.Memo,
            r.Label,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoCryptoWalletRepository(IMongoCollection<CryptoWalletRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<CryptoWallet> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<CryptoWallet>(source);
    }

    public async Task<CryptoWallet> CreateAsync(CryptoWallet entity)
    {
        var record = CryptoWalletRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return CryptoWalletRecordMapping.ToCryptoWallet(record);
    }

    async Task IRepository<CryptoWallet>.CreateAsync(CryptoWallet entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<CryptoWallet> entities)
    {
        var records = entities.Select(CryptoWalletRecordMapping.ToRecord);
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
        var record = CryptoWalletRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(CryptoWallet) instead.");
}

public sealed class MongoWithdrawalRepository : IWithdrawalRepository
{
    private readonly IMongoCollection<WithdrawalRecord> _collection;

    private static readonly Expression<Func<WithdrawalRecord, Withdrawal>> ToProjection = r =>
        new Withdrawal(
            r.Id.ToString(),
            r.OperationId,
            r.Type,
            r.StrawManAccountId,
            r.BankAccountId,
            r.CryptoWalletId,
            r.PaymentIds,
            r.CostDescription,
            r.CostAmount,
            r.PixProof != null && (r.PixProof.TransactionId != null || r.PixProof.AuthenticationCode != null)
                ? new PixProof(r.PixProof.TransactionId, r.PixProof.AuthenticationCode)
                : null,
            r.CryptoProof != null && r.CryptoProof.TransactionId != null
                ? new CryptoProof(r.CryptoProof.TransactionId)
                : null,
            r.PaymentsTotalAmount,
            r.NetAmount,
            r.CreatedAt);

    public MongoWithdrawalRepository(IMongoCollection<WithdrawalRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Withdrawal> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<Withdrawal>(source);
    }

    public async Task<Withdrawal> CreateAsync(Withdrawal entity)
    {
        var record = WithdrawalRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return WithdrawalRecordMapping.ToWithdrawal(record);
    }

    async Task IRepository<Withdrawal>.CreateAsync(Withdrawal entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<Withdrawal> entities)
    {
        var records = entities.Select(WithdrawalRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Withdrawal entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Withdrawal, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(w => ObjectId.Parse(w.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(Withdrawal entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var record = WithdrawalRecordMapping.ToRecord(entity);
        await _collection.ReplaceOneAsync(r => r.Id == objectId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Withdrawal) instead.");
}

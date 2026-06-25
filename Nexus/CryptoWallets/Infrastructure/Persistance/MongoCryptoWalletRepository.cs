using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.CryptoWallets.Infrastructure.Persistance;

public sealed class MongoCryptoWalletRepository : ICryptoWalletRepository
{
    private readonly IMongoCollection<CryptoWalletRecord> _collection;

    private static readonly Expression<Func<CryptoWalletRecord, CryptoWallet>> ToCryptoWalletProjection = r =>
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
                b.Splits.Select(s => new CryptoBalanceSplit(
                    s.AccountId,
                    s.Percentage,
                    s.Amount,
                    s.SplitKind)).ToList(),
                b.AppliedStrawManFeeIds,
                new CryptoBalanceOrigin(
                    b.Origin.OperationId,
                    b.Origin.OperatorId,
                    b.Origin.StrawManId))).ToList());

    public MongoCryptoWalletRepository(IMongoCollection<CryptoWalletRecord> collection)
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

using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Driver;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.CryptoWallets.Infrastructure.Persistance;

public sealed class MongoCryptoBalanceRepository : ICryptoBalanceRepository
{
    private readonly IMongoCollection<CryptoBalanceDocument> _collection;

    private static readonly Expression<Func<CryptoBalanceDocument, CryptoBalance>> ToCryptoBalanceProjection = d =>
        new CryptoBalance(
            d.Id,
            d.CryptoWalletId,
            d.Chain,
            d.Asset,
            d.Amount,
            d.TransferId,
            d.CreatedAt,
            d.Splits.Select(s => new CryptoBalanceSplit(
                s.AccountId,
                s.Percentage,
                s.Amount,
                s.SplitKind)).ToList(),
            new CryptoBalanceOrigin(
                d.Origin.OperationId,
                d.Origin.OperatorId));

    public MongoCryptoBalanceRepository(IMongoCollection<CryptoBalanceDocument> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<CryptoBalance> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToCryptoBalanceProjection);
        return new MongoAsyncQueryable<CryptoBalance>(source);
    }

    public async Task<CryptoBalance> CreateAsync(CryptoBalance entity)
    {
        var document = CryptoBalanceDocumentMapping.ToDocument(entity);
        await _collection.InsertOneAsync(document);
        return CryptoBalanceDocumentMapping.ToCryptoBalance(document);
    }

    async Task IRepository<CryptoBalance>.CreateAsync(CryptoBalance entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<CryptoBalance> entities)
    {
        var documents = entities.Select(CryptoBalanceDocumentMapping.ToDocument);
        return _collection.InsertManyAsync(documents);
    }

    public Task DeleteAsync(CryptoBalance entity) =>
        _collection.DeleteOneAsync(d => d.Id == entity.Id);

    public async Task<long> DeleteAsync(Expression<Func<CryptoBalance, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(b => b.Id).ToList();
        var result = await _collection.DeleteManyAsync(d => ids.Contains(d.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(CryptoBalance entity)
    {
        var document = CryptoBalanceDocumentMapping.ToDocument(entity);
        await _collection.ReplaceOneAsync(d => d.Id == entity.Id, document);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(CryptoBalance) instead.");
}

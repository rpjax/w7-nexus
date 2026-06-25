using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Driver;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Infrastructure.Mapping;
using Nexus.Database.Models;

namespace Nexus.BankAccounts.Infrastructure.Persistance;

public sealed class MongoBankBalanceRepository : IBankBalanceRepository
{
    private readonly IMongoCollection<BankBalanceDocument> _collection;

    private static readonly Expression<Func<BankBalanceDocument, BankBalance>> ToBankBalanceProjection = d =>
        new BankBalance(
            d.Id,
            d.BankAccountId,
            d.AmountBrl,
            d.TransferId,
            d.CreatedAt,
            d.Splits.Select(s => new BankBalanceSplit(
                s.AccountId,
                s.Percentage,
                s.Amount,
                s.SplitKind)).ToList(),
            new BankBalanceOrigin(
                d.Origin.OperationId,
                d.Origin.OperatorId));

    public MongoBankBalanceRepository(IMongoCollection<BankBalanceDocument> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<BankBalance> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToBankBalanceProjection);
        return new MongoAsyncQueryable<BankBalance>(source);
    }

    public async Task<BankBalance> CreateAsync(BankBalance entity)
    {
        var document = BankBalanceDocumentMapping.ToDocument(entity);
        await _collection.InsertOneAsync(document);
        return BankBalanceDocumentMapping.ToBankBalance(document);
    }

    async Task IRepository<BankBalance>.CreateAsync(BankBalance entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<BankBalance> entities)
    {
        var documents = entities.Select(BankBalanceDocumentMapping.ToDocument);
        return _collection.InsertManyAsync(documents);
    }

    public Task DeleteAsync(BankBalance entity) =>
        _collection.DeleteOneAsync(d => d.Id == entity.Id);

    public async Task<long> DeleteAsync(Expression<Func<BankBalance, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(b => b.Id).ToList();
        var result = await _collection.DeleteManyAsync(d => ids.Contains(d.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(BankBalance entity)
    {
        var document = BankBalanceDocumentMapping.ToDocument(entity);
        await _collection.ReplaceOneAsync(d => d.Id == entity.Id, document);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(BankBalance) instead.");
}

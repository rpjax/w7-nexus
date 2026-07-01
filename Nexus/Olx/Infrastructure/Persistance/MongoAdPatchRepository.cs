using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Infrastructure.Mapping;

namespace Nexus.Olx.Infrastructure.Persistance;

public sealed class MongoAdSpoofRepository : IAdSpoofRepository
{
    private readonly IMongoCollection<AdSpoofRecord> _collection;

    private static readonly Expression<Func<AdSpoofRecord, AdSpoof>> ToProjection = r =>
        new AdSpoof(
            r.Id.ToString(),
            r.OperationId,
            r.AdId,
            r.AdUrl,
            r.OperatorId,
            r.IsImpersonating,
            r.OriginalPrice,
            r.PromotionalPrice,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoAdSpoofRepository(IMongoCollection<AdSpoofRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<AdSpoof> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<AdSpoof>(source);
    }

    public async Task<AdSpoof> CreateAsync(AdSpoof entity)
    {
        var record = AdSpoofRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return AdSpoofRecordMapping.ToAdSpoof(record);
    }

    async Task IRepository<AdSpoof>.CreateAsync(AdSpoof entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<AdSpoof> entities)
    {
        var records = entities.Select(AdSpoofRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(AdSpoof entity) =>
        _collection.DeleteOneAsync(r => r.Id == ObjectId.Parse(entity.Id));

    public async Task<long> DeleteAsync(Expression<Func<AdSpoof, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(s => ObjectId.Parse(s.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(AdSpoof entity)
    {
        var existing = await _collection.Find(r => r.Id == ObjectId.Parse(entity.Id)).FirstOrDefaultAsync();
        var record = new AdSpoofRecord
        {
            Id = existing?.Id ?? ObjectId.Parse(entity.Id),
            OperationId = entity.OperationId,
            AdId = entity.AdId,
            AdUrl = entity.AdUrl,
            OperatorId = entity.OperatorId,
            IsImpersonating = entity.IsImpersonating,
            OriginalPrice = entity.OriginalPrice,
            PromotionalPrice = entity.PromotionalPrice,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
        await _collection.ReplaceOneAsync(r => r.Id == record.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(AdSpoof) instead.");
}

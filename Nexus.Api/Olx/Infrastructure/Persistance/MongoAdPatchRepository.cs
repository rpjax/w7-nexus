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

public sealed class MongoAdPatchRepository : IAdPatchRepository
{
    private readonly IMongoCollection<OlxAdPatchRecord> _collection;

    private static readonly Expression<Func<OlxAdPatchRecord, AdPatch>> ToProjection = r =>
        new AdPatch(
            r.Id.ToString(),
            r.OperationId,
            r.AdId,
            r.AdUrl,
            r.OperatorId ?? string.Empty,
            r.IsImpersonating,
            r.OriginalPrice,
            r.PromotionalPrice,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoAdPatchRepository(IMongoCollection<OlxAdPatchRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<AdPatch> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<AdPatch>(source);
    }

    public async Task<AdPatch> CreateAsync(AdPatch entity)
    {
        var record = OlxAdPatchRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return OlxAdPatchRecordMapping.ToAdPatch(record);
    }

    async Task IRepository<AdPatch>.CreateAsync(AdPatch entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<AdPatch> entities)
    {
        var records = entities.Select(OlxAdPatchRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(AdPatch entity) =>
        _collection.DeleteOneAsync(r => r.Id == ObjectId.Parse(entity.Id));

    public async Task<long> DeleteAsync(Expression<Func<AdPatch, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(s => ObjectId.Parse(s.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.Id));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(AdPatch entity)
    {
        var existing = await _collection.Find(r => r.Id == ObjectId.Parse(entity.Id)).FirstOrDefaultAsync();
        var record = new OlxAdPatchRecord
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
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(AdPatch) instead.");
}

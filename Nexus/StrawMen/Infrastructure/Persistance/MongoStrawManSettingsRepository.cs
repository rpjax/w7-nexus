using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.StrawMen.Aggregates;
using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Infrastructure.Mapping;

namespace Nexus.StrawMen.Infrastructure.Persistance;

public sealed class MongoStrawManSettingsRepository : IStrawManSettingsRepository
{
    private readonly IMongoCollection<StrawManSettingsRecord> _collection;

    private static readonly Expression<Func<StrawManSettingsRecord, StrawManSettings>> ToProjection = r =>
        new StrawManSettings(r.StrawManId, r.MovementFeePercentage, r.UpdatedAt, r.UpdatedByAdminId);

    public MongoStrawManSettingsRepository(IMongoCollection<StrawManSettingsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<StrawManSettings> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<StrawManSettings>(source);
    }

    public async Task<StrawManSettings> CreateAsync(StrawManSettings entity)
    {
        var record = StrawManSettingsRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return StrawManSettingsRecordMapping.ToSettings(record);
    }

    async Task IRepository<StrawManSettings>.CreateAsync(StrawManSettings entity) =>
        await CreateAsync(entity);

    public Task CreateAsync(IEnumerable<StrawManSettings> entities)
    {
        var records = entities.Select(StrawManSettingsRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(StrawManSettings entity) =>
        _collection.DeleteOneAsync(r => r.StrawManId == entity.StrawManId);

    public async Task<long> DeleteAsync(Expression<Func<StrawManSettings, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var strawManIds = toDelete.Select(s => s.StrawManId).ToList();
        var result = await _collection.DeleteManyAsync(r => strawManIds.Contains(r.StrawManId));
        return result.DeletedCount;
    }

    public async Task UpdateAsync(StrawManSettings entity)
    {
        var existing = await _collection.Find(r => r.StrawManId == entity.StrawManId).FirstOrDefaultAsync();
        var record = new StrawManSettingsRecord
        {
            Id = existing?.Id ?? ObjectId.GenerateNewId(),
            StrawManId = entity.StrawManId,
            MovementFeePercentage = entity.MovementFeePercentage,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByAdminId = entity.UpdatedByAdminId,
        };
        await _collection.ReplaceOneAsync(r => r.StrawManId == entity.StrawManId, record);
    }

    public Task<long> UpdateAsync(Expression expression) =>
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(StrawManSettings) instead.");
}

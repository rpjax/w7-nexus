using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Infrastructure.Mapping;

namespace Nexus.Gateways.Infrastructure.Persistance;

public sealed class MongoGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
{
    private readonly IMongoCollection<GatewayCredentialsGroupRecord> _collection;

    private static readonly Expression<Func<GatewayCredentialsGroupRecord, GatewayCredentialsGroup>> ToProjection = r =>
        new GatewayCredentialsGroup(
            r.Id.ToString(),
            r.Name,
            r.GatewayCredentialsIds,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoGatewayCredentialsGroupRepository(IMongoCollection<GatewayCredentialsGroupRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<GatewayCredentialsGroup>(source);
    }

    public async Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity)
    {
        var record = GatewayCredentialsGroupRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return GatewayCredentialsGroupRecordMapping.ToGroup(record);
    }

    async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities)
    {
        var records = entities.Select(GatewayCredentialsGroupRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(GatewayCredentialsGroup entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(g => ObjectId.Parse(g.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public Task UpdateAsync(GatewayCredentialsGroup entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var update = Builders<GatewayCredentialsGroupRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.GatewayCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.Id == objectId, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(GatewayCredentialsGroup) instead.");
    }
}

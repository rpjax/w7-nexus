using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Charges.Application;
using Nexus.Charges.Entities;
using Nexus.Database.Models;

namespace Nexus.Charges.Infrastructure;

public sealed class GatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
{
    private readonly IMongoCollection<GatewayCredentialsGroupRecord> _collection;

    private static readonly Expression<Func<GatewayCredentialsGroupRecord, GatewayCredentialsGroup>> ToProjection = r =>
        new GatewayCredentialsGroup(
            r.GroupId,
            r.Name,
            r.GatewayCredentialsIds,
            r.CreatedAt,
            r.UpdatedAt);

    public GatewayCredentialsGroupRepository(IMongoCollection<GatewayCredentialsGroupRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<GatewayCredentialsGroup>(source);
    }

    public Task CreateAsync(GatewayCredentialsGroup entity)
    {
        var record = ToRecord(entity);
        return _collection.InsertOneAsync(record);
    }

    public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities)
    {
        var records = entities.Select(ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(GatewayCredentialsGroup entity)
    {
        return _collection.DeleteOneAsync(r => r.GroupId == entity.Id);
    }

    public async Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(g => g.Id).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.GroupId));
        return result.DeletedCount;
    }

    public Task UpdateAsync(GatewayCredentialsGroup entity)
    {
        var update = Builders<GatewayCredentialsGroupRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.GatewayCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.GroupId == entity.Id, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(GatewayCredentialsGroup) instead.");
    }

    private static GatewayCredentialsGroupRecord ToRecord(GatewayCredentialsGroup group)
    {
        return new GatewayCredentialsGroupRecord
        {
            Id = ObjectId.GenerateNewId(),
            GroupId = group.Id,
            Name = group.Name,
            GatewayCredentialsIds = group.GatewayCredentialsIds.ToList(),
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}

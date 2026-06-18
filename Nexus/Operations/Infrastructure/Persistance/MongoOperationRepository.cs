using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Infrastructure.Mapping;

namespace Nexus.Operations.Infrastructure.Persistance;

public sealed class MongoOperationRepository : IOperationRepository
{
    private readonly IMongoCollection<OperationRecord> _collection;

    private static readonly Expression<Func<OperationRecord, Operation>> ToOperationProjection = r =>
        new Operation(
            r.Id.ToString(),
            r.Name,
            r.Description,
            r.AdministratorIds,
            r.StrawManIds,
            r.GatewaySelectionStrategy,
            r.GatewayCredentialsIds,
            r.GatewayCredentialsGroupIds,
            r.CreatedAt,
            r.UpdatedAt);

    public MongoOperationRepository(IMongoCollection<OperationRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Operation> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToOperationProjection);
        return new MongoAsyncQueryable<Operation>(source);
    }

    public async Task<Operation> CreateAsync(Operation entity)
    {
        var record = OperationRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return OperationRecordMapping.ToOperation(record);
    }

    async Task IRepository<Operation>.CreateAsync(Operation entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<Operation> entities)
    {
        var records = entities.Select(OperationRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Operation entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var objectIds = toDelete.Select(o => ObjectId.Parse(o.Id)).ToList();
        var result = await _collection.DeleteManyAsync(r => objectIds.Contains(r.Id));
        return result.DeletedCount;
    }

    public Task UpdateAsync(Operation entity)
    {
        var objectId = ObjectId.Parse(entity.Id);
        var update = Builders<OperationRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.Description, entity.Description)
            .Set(r => r.AdministratorIds, entity.AdministratorIds.ToList())
            .Set(r => r.StrawManIds, entity.StrawManIds.ToList())
            .Set(r => r.GatewaySelectionStrategy, entity.GatewaySelectionStrategy)
            .Set(r => r.GatewayCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.GatewayCredentialsGroupIds, entity.GatewayCredentialsGroupIds.ToList())
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.Id == objectId, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Operation) instead.");
    }
}

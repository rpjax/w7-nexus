using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Legacy.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;

namespace Nexus.Operations.Infrastructure;

public sealed class OperationRepository : IOperationRepository
{
    private readonly IMongoCollection<OperationRecord> _collection;

    private static readonly Expression<Func<OperationRecord, Operation>> ToOperationProjection = r =>
        new Operation(
            r.OperationId,
            r.Name,
            r.Description,
            r.Operators,
            r.StrawManIds,
            r.ManuallySetChargeCredentials,
            r.ChargeCredentialsIds,
            r.CreatedAt,
            r.UpdatedAt);

    public OperationRepository(IMongoCollection<OperationRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<Operation> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToOperationProjection);
        return new MongoAsyncQueryable<Operation>(source);
    }

    public Task CreateAsync(Operation entity)
    {
        var record = ToRecord(entity);
        return _collection.InsertOneAsync(record);
    }

    public Task CreateAsync(IEnumerable<Operation> entities)
    {
        var records = entities.Select(ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(Operation entity)
    {
        return _collection.DeleteOneAsync(r => r.OperationId == entity.Id);
    }

    public async Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(o => o.Id).ToList();
        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.OperationId));
        return result.DeletedCount;
    }

    public Task UpdateAsync(Operation entity)
    {
        var update = Builders<OperationRecord>.Update
            .Set(r => r.Name, entity.Name)
            .Set(r => r.Description, entity.Description)
            .Set(r => r.Operators, entity.OperatorIds.ToList())
            .Set(r => r.StrawManIds, entity.StrawManIds.ToList())
            .Set(r => r.ManuallySetChargeCredentials, entity.ManuallySetChargeCredentials)
            .Set(r => r.ChargeCredentialsIds, entity.GatewayCredentialsIds.ToList())
            .Set(r => r.CreatedAt, entity.CreatedAt)
            .Set(r => r.UpdatedAt, entity.UpdatedAt);

        return _collection.UpdateOneAsync(r => r.OperationId == entity.Id, update);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load aggregate(s) and call UpdateAsync(Operation) instead.");
    }

    private static OperationRecord ToRecord(Operation operation)
    {
        return new OperationRecord
        {
            Id = ObjectId.GenerateNewId(),
            OperationId = operation.Id,
            Name = operation.Name,
            Description = operation.Description,
            Operators = operation.OperatorIds.ToList(),
            StrawManIds = operation.StrawManIds.ToList(),
            ManuallySetChargeCredentials = operation.ManuallySetChargeCredentials,
            ChargeCredentialsIds = operation.GatewayCredentialsIds.ToList(),
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt
        };
    }
}

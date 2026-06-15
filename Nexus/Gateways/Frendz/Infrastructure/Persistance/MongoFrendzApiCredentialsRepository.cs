using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Infrastructure.Mapping;

namespace Nexus.Gateways.Frendz.Infrastructure.Persistance;

public sealed class MongoFrendzApiCredentialsRepository : IFrendzApiCredentialsRepository
{
    private readonly IMongoCollection<FrendzApiCredentialsRecord> _collection;

    private static readonly Expression<Func<FrendzApiCredentialsRecord, FrendzApiCredentials>> ToProjection = r =>
        new FrendzApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            Token = r.Token,
            Enabled = r.Enabled
        };

    public MongoFrendzApiCredentialsRepository(IMongoCollection<FrendzApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<FrendzApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<FrendzApiCredentials>(source);
    }

    public async Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity)
    {
        var record = FrendzApiCredentialsRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return FrendzApiCredentialsRecordMapping.ToModel(record);
    }

    async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities)
    {
        var records = entities.Select(FrendzApiCredentialsRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(FrendzApiCredentials entity)
    {
        if (!ObjectId.TryParse(entity.Id, out var objectId))
            return Task.CompletedTask;

        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate)
    {
        var toDelete = AsQueryable().Where(predicate).ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete
            .Select(c => ObjectId.TryParse(c.Id, out var oid) ? oid : ObjectId.Empty)
            .Where(oid => oid != ObjectId.Empty)
            .ToList();

        if (ids.Count == 0)
            return 0;

        var result = await _collection.DeleteManyAsync(r => ids.Contains(r.Id));
        return result.DeletedCount;
    }

    public Task UpdateAsync(FrendzApiCredentials entity)
    {
        var record = FrendzApiCredentialsRecordMapping.ToRecord(entity);
        return _collection.ReplaceOneAsync(r => r.Id == record.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load credential(s) and call UpdateAsync(FrendzApiCredentials) instead.");
    }
}

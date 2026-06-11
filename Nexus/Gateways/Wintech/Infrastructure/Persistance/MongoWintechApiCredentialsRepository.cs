using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Services.Contracts;
using Nexus.Gateways.Wintech.Infrastructure.Mapping;

namespace Nexus.Gateways.Wintech.Infrastructure.Persistance;

public sealed class MongoWintechApiCredentialsRepository : IWintechApiCredentialsRepository
{
    private readonly IMongoCollection<WintechApiCredentialsRecord> _collection;

    private static readonly Expression<Func<WintechApiCredentialsRecord, WintechApiCredentials>> ToProjection = r =>
        new WintechApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            PublicKey = r.PublicKey,
            SecretKey = r.SecretKey,
            Enabled = r.Enabled
        };

    public MongoWintechApiCredentialsRepository(IMongoCollection<WintechApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<WintechApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<WintechApiCredentials>(source);
    }

    public async Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity)
    {
        var record = WintechApiCredentialsRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return WintechApiCredentialsRecordMapping.ToModel(record);
    }

    async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<WintechApiCredentials> entities)
    {
        var records = entities.Select(WintechApiCredentialsRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(WintechApiCredentials entity)
    {
        if (!ObjectId.TryParse(entity.Id, out var objectId))
            return Task.CompletedTask;

        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate)
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

    public Task UpdateAsync(WintechApiCredentials entity)
    {
        var record = WintechApiCredentialsRecordMapping.ToRecord(entity);
        return _collection.ReplaceOneAsync(r => r.Id == record.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load credential(s) and call UpdateAsync(WintechApiCredentials) instead.");
    }
}

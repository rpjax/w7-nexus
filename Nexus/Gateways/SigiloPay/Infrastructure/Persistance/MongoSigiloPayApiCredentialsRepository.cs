using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services.Contracts;
using Nexus.Gateways.SigiloPay.Infrastructure.Mapping;

namespace Nexus.Gateways.SigiloPay.Infrastructure.Persistance;

public sealed class MongoSigiloPayApiCredentialsRepository : ISigiloPayApiCredentialsRepository
{
    private readonly IMongoCollection<SigiloPayApiCredentialsRecord> _collection;

    private static readonly Expression<Func<SigiloPayApiCredentialsRecord, SigiloPayApiCredentials>> ToProjection = r =>
        new SigiloPayApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            PublicKey = r.PublicKey,
            SecretKey = r.SecretKey,
            Enabled = r.Enabled
        };

    public MongoSigiloPayApiCredentialsRepository(IMongoCollection<SigiloPayApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<SigiloPayApiCredentials>(source);
    }

    public async Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity)
    {
        var record = SigiloPayApiCredentialsRecordMapping.ToRecord(entity);
        await _collection.InsertOneAsync(record);
        return SigiloPayApiCredentialsRecordMapping.ToModel(record);
    }

    async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities)
    {
        var records = entities.Select(SigiloPayApiCredentialsRecordMapping.ToRecord);
        return _collection.InsertManyAsync(records);
    }

    public Task DeleteAsync(SigiloPayApiCredentials entity)
    {
        if (!ObjectId.TryParse(entity.Id, out var objectId))
            return Task.CompletedTask;

        return _collection.DeleteOneAsync(r => r.Id == objectId);
    }

    public async Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate)
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

    public Task UpdateAsync(SigiloPayApiCredentials entity)
    {
        var record = SigiloPayApiCredentialsRecordMapping.ToRecord(entity);
        return _collection.ReplaceOneAsync(r => r.Id == record.Id, record);
    }

    public Task<long> UpdateAsync(Expression expression)
    {
        throw new NotSupportedException(
            "Bulk update by expression is not supported. Load credential(s) and call UpdateAsync(SigiloPayApiCredentials) instead.");
    }
}

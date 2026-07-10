using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Infrastructure.Mapping;

namespace Nexus.Scripts.Infrastructure.Persistance;

public sealed class MongoReleaseRepository : IReleaseRepository
{
    private readonly IMongoCollection<ReleaseRecord> _collection;

    public MongoReleaseRepository(IMongoCollection<ReleaseRecord> collection)
    {
        _collection = collection;
    }

    public async Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var record = await _collection
            .Find(Builders<ReleaseRecord>.Filter.Eq(r => r.Id, objectId))
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ReleaseRecordMapping.ToRelease(record);
    }

    public async Task<IReadOnlyList<Release>> GetByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        var objectIds = ids
            .Select(id => ObjectId.TryParse(id, out var objectId) ? objectId : (ObjectId?)null)
            .Where(objectId => objectId.HasValue)
            .Select(objectId => objectId!.Value)
            .ToList();

        if (objectIds.Count == 0)
            return Array.Empty<Release>();

        var records = await _collection
            .Find(Builders<ReleaseRecord>.Filter.In(r => r.Id, objectIds))
            .ToListAsync(cancellationToken);

        return records.Select(ReleaseRecordMapping.ToRelease).ToList();
    }

    public async Task<IReadOnlyList<Release>> GetByScriptIdsAndVersionAsync(
        IEnumerable<string> scriptIds,
        SemanticVersion version,
        CancellationToken cancellationToken = default)
    {
        var ids = scriptIds.Distinct(StringComparer.Ordinal).ToList();

        if (ids.Count == 0)
            return Array.Empty<Release>();

        var filter = Builders<ReleaseRecord>.Filter.And(
            Builders<ReleaseRecord>.Filter.In(r => r.ScriptId, ids),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Major, version.Major),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Minor, version.Minor),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Patch, version.Patch));

        var records = await _collection.Find(filter).ToListAsync(cancellationToken);
        return records.Select(ReleaseRecordMapping.ToRelease).ToList();
    }

    public async Task<Release?> GetLatestByScriptIdAsync(string scriptId, CancellationToken cancellationToken = default)
    {
        var record = await _collection
            .Find(Builders<ReleaseRecord>.Filter.Eq(r => r.ScriptId, scriptId))
            .SortByDescending(r => r.Major)
            .ThenByDescending(r => r.Minor)
            .ThenByDescending(r => r.Patch)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ReleaseRecordMapping.ToRelease(record);
    }

    public async Task<IReadOnlyList<Release>> ListByScriptIdAsync(
        string scriptId,
        CancellationToken cancellationToken = default)
    {
        var records = await _collection
            .Find(Builders<ReleaseRecord>.Filter.Eq(r => r.ScriptId, scriptId))
            .SortByDescending(r => r.Major)
            .ThenByDescending(r => r.Minor)
            .ThenByDescending(r => r.Patch)
            .ToListAsync(cancellationToken);

        return records.Select(ReleaseRecordMapping.ToRelease).ToList();
    }

    public async Task<Release> InsertAsync(Release release, CancellationToken cancellationToken = default)
    {
        var record = ReleaseRecordMapping.ToRecord(release);
        await _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
        return ReleaseRecordMapping.ToRelease(record);
    }

    public async Task UpdateAsync(Release release, CancellationToken cancellationToken = default)
    {
        var record = ReleaseRecordMapping.ToRecord(release);
        await _collection.ReplaceOneAsync(
            Builders<ReleaseRecord>.Filter.Eq(r => r.Id, record.Id),
            record,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await _collection.DeleteOneAsync(
            Builders<ReleaseRecord>.Filter.Eq(r => r.Id, objectId),
            cancellationToken);

        return result.DeletedCount > 0;
    }

    public async Task<bool> VersionExistsAsync(
        string scriptId,
        SemanticVersion version,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ReleaseRecord>.Filter.And(
            Builders<ReleaseRecord>.Filter.Eq(r => r.ScriptId, scriptId),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Major, version.Major),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Minor, version.Minor),
            Builders<ReleaseRecord>.Filter.Eq(r => r.Patch, version.Patch));

        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
}

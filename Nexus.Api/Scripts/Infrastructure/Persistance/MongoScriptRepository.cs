using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Infrastructure.Mapping;

namespace Nexus.Scripts.Infrastructure.Persistance;

public sealed class MongoScriptRepository : IScriptRepository
{
    private readonly IMongoCollection<ScriptRecord> _collection;

    public MongoScriptRepository(IMongoCollection<ScriptRecord> collection)
    {
        _collection = collection;
    }

    public async Task<Script?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var record = await _collection
            .Find(Builders<ScriptRecord>.Filter.Eq(r => r.Id, objectId))
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ScriptRecordMapping.ToScript(record);
    }

    public async Task<Script?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScriptRecord>.Filter.Regex(
            r => r.Name,
            new BsonRegularExpression($"^{RegexEscape(name.Trim())}$", "i"));

        var record = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : ScriptRecordMapping.ToScript(record);
    }

    public async Task<IReadOnlyList<Script>> ListWithHostPatternsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScriptRecord>.Filter.SizeGt(r => r.HostPatterns, 0);
        var records = await _collection.Find(filter).ToListAsync(cancellationToken);
        return records.Select(ScriptRecordMapping.ToScript).ToList();
    }

    public async Task<(IReadOnlyList<Script> Items, int Total)> SearchAsync(
        string? keyword,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildSearchFilter(keyword);
        var total = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var records = await _collection
            .Find(filter)
            .SortBy(r => r.Name)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return (records.Select(ScriptRecordMapping.ToScript).ToList(), total);
    }

    public async Task<Script> InsertAsync(Script script, CancellationToken cancellationToken = default)
    {
        var record = ScriptRecordMapping.ToRecord(script);
        await _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
        return ScriptRecordMapping.ToScript(record);
    }

    public async Task UpdateAsync(Script script, CancellationToken cancellationToken = default)
    {
        var record = ScriptRecordMapping.ToRecord(script);
        await _collection.ReplaceOneAsync(
            Builders<ScriptRecord>.Filter.Eq(r => r.Id, record.Id),
            record,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScriptRecord>.Filter.Regex(
            r => r.Name,
            new BsonRegularExpression($"^{RegexEscape(name.Trim())}$", "i"));

        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    private static FilterDefinition<ScriptRecord> BuildSearchFilter(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Builders<ScriptRecord>.Filter.Empty;

        var escaped = RegexEscape(keyword.Trim());
        var regex = new BsonRegularExpression(escaped, "i");

        return Builders<ScriptRecord>.Filter.Or(
            Builders<ScriptRecord>.Filter.Regex(r => r.Name, regex),
            Builders<ScriptRecord>.Filter.Regex(r => r.Description!, regex));
    }

    private static string RegexEscape(string value) =>
        System.Text.RegularExpressions.Regex.Escape(value);
}

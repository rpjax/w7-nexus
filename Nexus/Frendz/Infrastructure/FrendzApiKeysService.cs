using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Frendz.Application;
using Nexus.Frendz.Application.Models;

namespace Nexus.Frendz.Infrastructure;

public class FrendzApiKeysService : IFrendzApiKeysService
{
    private IMongoCollection<FrendzApiCredentialsRecord> _credentialsCollection { get; }

    public FrendzApiKeysService(IMongoCollection<FrendzApiCredentialsRecord> credentialsCollection)
    {
        _credentialsCollection = credentialsCollection;
    }

    public async Task<FrendzApiCredentials?> GetRandomCredentialsAsync()
    {
        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Empty;
        var count = await _credentialsCollection.CountDocumentsAsync(filter);
        if (count == 0)
            return null;

        var skip = Random.Shared.Next(0, (int)count);
        var record = await _credentialsCollection
            .Find(filter)
            .Skip(skip)
            .Limit(1)
            .FirstOrDefaultAsync();

        return record is null ? null : ToModel(record);
    }

    public async Task<FrendzApiCredentials> AddCredentialsAsync(string? strawManId, string token, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var normalizedStrawMan = string.IsNullOrWhiteSpace(strawManId) ? null : strawManId.Trim();

        var record = new FrendzApiCredentialsRecord
        {
            Id = ObjectId.GenerateNewId(),
            StrawManId = normalizedStrawMan,
            Name = name ?? string.Empty,
            Token = token
        };

        await _credentialsCollection.InsertOneAsync(record);
        return ToModel(record);
    }

    public async Task<bool> UpdateCredentialsAsync(string id, string? strawManId, string token, string name)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var objectId))
            return false;

        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var normalizedStrawMan = string.IsNullOrWhiteSpace(strawManId) ? null : strawManId.Trim();

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var update = Builders<FrendzApiCredentialsRecord>.Update
            .Set(r => r.StrawManId, normalizedStrawMan)
            .Set(r => r.Token, token)
            .Set(r => r.Name, name ?? string.Empty);

        var result = await _credentialsCollection.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteCredentialsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var objectId))
            return false;

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var result = await _credentialsCollection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    private static FrendzApiCredentials ToModel(FrendzApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            Token = record.Token
        };
}

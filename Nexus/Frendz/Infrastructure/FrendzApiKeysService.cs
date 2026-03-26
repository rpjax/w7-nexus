using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Frendz.Application;

namespace Nexus.Frendz.Infrastructure;

public class FrendzApiKeysService : IFrendzApiKeysService
{
    private IMongoCollection<FrendzApiCredentialsRecord> _credentialsCollection { get; }

    public FrendzApiKeysService(IMongoCollection<FrendzApiCredentialsRecord> credentialsCollection)
    {
        _credentialsCollection = credentialsCollection;
    }

    public async Task<FredzApiCredentials?> GetRandomCredentialsAsync()
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

    public async Task<FredzApiCredentials> AddCredentialsAsync(string token, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var record = new FrendzApiCredentialsRecord
        {
            Id = ObjectId.GenerateNewId(),
            Name = name ?? string.Empty,
            Token = token
        };

        await _credentialsCollection.InsertOneAsync(record);
        return ToModel(record);
    }

    public async Task<bool> DeleteCredentialsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var objectId))
            return false;

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var result = await _credentialsCollection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    private static FredzApiCredentials ToModel(FrendzApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            Name = record.Name,
            Token = record.Token
        };
}

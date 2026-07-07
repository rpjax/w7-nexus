using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Infrastructure.Mapping;

internal static class FrendzApiCredentialsRecordMapping
{
    public static FrendzApiCredentials ToModel(FrendzApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            Token = record.Token,
            Enabled = record.Enabled
        };

    public static FrendzApiCredentialsRecord ToRecord(FrendzApiCredentials entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id) || !ObjectId.TryParse(entity.Id, out var objectId))
            objectId = ObjectId.GenerateNewId();

        return new FrendzApiCredentialsRecord
        {
            Id = objectId,
            StrawManId = entity.StrawManId,
            Name = entity.Name,
            Token = entity.Token,
            Enabled = entity.Enabled
        };
    }
}

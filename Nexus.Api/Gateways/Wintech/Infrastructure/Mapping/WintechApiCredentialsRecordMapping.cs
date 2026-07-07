using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Wintech.Infrastructure.Mapping;

internal static class WintechApiCredentialsRecordMapping
{
    public static WintechApiCredentials ToModel(WintechApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            PublicKey = record.PublicKey,
            SecretKey = record.SecretKey,
            Enabled = record.Enabled
        };

    public static WintechApiCredentialsRecord ToRecord(WintechApiCredentials entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id) || !ObjectId.TryParse(entity.Id, out var objectId))
            objectId = ObjectId.GenerateNewId();

        return new WintechApiCredentialsRecord
        {
            Id = objectId,
            StrawManId = entity.StrawManId,
            Name = entity.Name,
            PublicKey = entity.PublicKey,
            SecretKey = entity.SecretKey,
            Enabled = entity.Enabled
        };
    }
}

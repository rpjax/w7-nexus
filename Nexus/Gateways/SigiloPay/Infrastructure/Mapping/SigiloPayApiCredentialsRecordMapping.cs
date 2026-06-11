using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Infrastructure.Mapping;

internal static class SigiloPayApiCredentialsRecordMapping
{
    public static SigiloPayApiCredentials ToModel(SigiloPayApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            PublicKey = record.PublicKey,
            SecretKey = record.SecretKey,
            Enabled = record.Enabled
        };

    public static SigiloPayApiCredentialsRecord ToRecord(SigiloPayApiCredentials entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id) || !ObjectId.TryParse(entity.Id, out var objectId))
            objectId = ObjectId.GenerateNewId();

        return new SigiloPayApiCredentialsRecord
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

using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Gateways.Aggregates;

namespace Nexus.Gateways.Infrastructure.Mapping;

internal static class GatewayCredentialsGroupRecordMapping
{
    public static GatewayCredentialsGroup ToGroup(GatewayCredentialsGroupRecord record) =>
        new(
            record.Id.ToString(),
            record.Name,
            record.GatewayCredentialsIds,
            record.CreatedAt,
            record.UpdatedAt);

    public static GatewayCredentialsGroupRecord ToRecord(GatewayCredentialsGroup group)
    {
        return new GatewayCredentialsGroupRecord
        {
            Id = string.IsNullOrWhiteSpace(group.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(group.Id),
            Name = group.Name,
            GatewayCredentialsIds = group.GatewayCredentialsIds.ToList(),
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}

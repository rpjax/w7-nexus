using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Gateways.Aggregates;

namespace Nexus.Gateways.Infrastructure.Mapping;

internal static class GatewayCredentialsGroupRecordMapping
{
    public static GatewayCredentialsGroup ToGroup(GatewayCredentialsGroupRecord record) =>
        new(
            record.GroupId,
            record.Name,
            record.GatewayCredentialsIds,
            record.CreatedAt,
            record.UpdatedAt);

    public static GatewayCredentialsGroupRecord ToRecord(GatewayCredentialsGroup group)
    {
        var groupId = string.IsNullOrWhiteSpace(group.Id)
            ? Guid.NewGuid().ToString("N")
            : group.Id;

        return new GatewayCredentialsGroupRecord
        {
            Id = ObjectId.GenerateNewId(),
            GroupId = groupId,
            Name = group.Name,
            GatewayCredentialsIds = group.GatewayCredentialsIds.ToList(),
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}

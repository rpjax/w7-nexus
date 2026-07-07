using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Infrastructure.Mapping;

internal static class OperationRecordMapping
{
    public static Operation ToOperation(OperationRecord record) =>
        new(
            record.Id.ToString(),
            record.Name,
            record.Description,
            record.AdministratorIds,
            record.StrawManIds,
            record.GatewaySelectionStrategy,
            record.GatewayCredentialsIds,
            record.GatewayCredentialsGroupIds,
            record.CreatedAt,
            record.UpdatedAt);

    public static OperationRecord ToRecord(Operation operation)
    {
        return new OperationRecord
        {
            Id = string.IsNullOrWhiteSpace(operation.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(operation.Id),
            Name = operation.Name,
            Description = operation.Description,
            AdministratorIds = operation.AdministratorIds.ToList(),
            StrawManIds = operation.StrawManIds.ToList(),
            GatewaySelectionStrategy = operation.GatewaySelectionStrategy,
            GatewayCredentialsIds = operation.GatewayCredentialsIds.ToList(),
            GatewayCredentialsGroupIds = operation.GatewayCredentialsGroupIds.ToList(),
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt
        };
    }
}

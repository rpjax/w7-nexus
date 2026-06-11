using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Infrastructure.Mapping;

internal static class OperationRecordMapping
{
    public static Operation ToOperation(OperationRecord record) =>
        new(
            record.OperationId,
            record.Name,
            record.Description,
            record.AdministratorIds,
            record.CreatedAt,
            record.UpdatedAt);

    public static OperationRecord ToRecord(Operation operation)
    {
        var operationId = string.IsNullOrWhiteSpace(operation.Id)
            ? Guid.NewGuid().ToString("N")
            : operation.Id;

        return new OperationRecord
        {
            Id = ObjectId.GenerateNewId(),
            OperationId = operationId,
            Name = operation.Name,
            Description = operation.Description,
            AdministratorIds = operation.AdministratorIds.ToList(),
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt
        };
    }
}

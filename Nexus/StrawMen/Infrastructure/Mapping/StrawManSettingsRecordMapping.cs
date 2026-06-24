using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.StrawMen.Aggregates;

namespace Nexus.StrawMen.Infrastructure.Mapping;

internal static class StrawManSettingsRecordMapping
{
    public static StrawManSettings ToSettings(StrawManSettingsRecord record) =>
        new(record.StrawManId, record.MovementFeePercentage, record.UpdatedAt, record.UpdatedByAdminId);

    public static StrawManSettingsRecord ToRecord(StrawManSettings entity) =>
        new()
        {
            Id = ObjectId.GenerateNewId(),
            StrawManId = entity.StrawManId,
            MovementFeePercentage = entity.MovementFeePercentage,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByAdminId = entity.UpdatedByAdminId,
        };
}

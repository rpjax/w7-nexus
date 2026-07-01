using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Olx.Aggregates;

namespace Nexus.Olx.Infrastructure.Mapping;

internal static class OlxAdPatchRecordMapping
{
    public static AdPatch ToAdPatch(OlxAdPatchRecord record) =>
        new(
            record.Id.ToString(),
            record.OperationId,
            record.AdId,
            record.AdUrl,
            record.OperatorId,
            record.IsImpersonating,
            record.OriginalPrice,
            record.PromotionalPrice,
            record.CreatedAt,
            record.UpdatedAt);

    public static OlxAdPatchRecord ToRecord(AdPatch entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            OperationId = entity.OperationId,
            AdId = entity.AdId,
            AdUrl = entity.AdUrl,
            OperatorId = entity.OperatorId,
            IsImpersonating = entity.IsImpersonating,
            OriginalPrice = entity.OriginalPrice,
            PromotionalPrice = entity.PromotionalPrice,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
}

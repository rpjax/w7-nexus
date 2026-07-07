using Nexus.Olx.Aggregates;
using AdminAdPatchDetails = Nexus.Olx.Application.Responses.Administrator.Models.AdPatchDetails;
using OperatorAdPatchDetails = Nexus.Olx.Application.Responses.Operator.Models.AdPatchDetails;

namespace Nexus.Olx.Application.Mapping;

internal static class AdPatchDetailsMapper
{
    public static AdminAdPatchDetails ToAdministratorDetails(AdPatch patch) =>
        new()
        {
            Id = patch.Id,
            OperationId = patch.OperationId,
            AdId = patch.AdId,
            AdUrl = patch.AdUrl,
            OperatorId = patch.OperatorId,
            IsImpersonating = patch.IsImpersonating,
            OriginalPrice = patch.OriginalPrice,
            PromotionalPrice = patch.PromotionalPrice,
            CreatedAt = patch.CreatedAt,
            UpdatedAt = patch.UpdatedAt,
        };

    public static OperatorAdPatchDetails ToOperatorDetails(AdPatch patch) =>
        new()
        {
            Id = patch.Id,
            OperationId = patch.OperationId,
            AdId = patch.AdId,
            AdUrl = patch.AdUrl,
            IsImpersonating = patch.IsImpersonating,
            OriginalPrice = patch.OriginalPrice,
            PromotionalPrice = patch.PromotionalPrice,
            CreatedAt = patch.CreatedAt,
            UpdatedAt = patch.UpdatedAt,
        };
}

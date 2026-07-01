using Nexus.Olx.Aggregates;
using AdminAdSpoofDetails = Nexus.Olx.Application.Responses.Administrator.Models.AdSpoofDetails;
using OperatorAdSpoofDetails = Nexus.Olx.Application.Responses.Operator.Models.AdSpoofDetails;

namespace Nexus.Olx.Application.Mapping;

internal static class AdSpoofDetailsMapper
{
    public static AdminAdSpoofDetails ToAdministratorDetails(AdSpoof spoof) =>
        new()
        {
            Id = spoof.Id,
            OperationId = spoof.OperationId,
            AdId = spoof.AdId,
            AdUrl = spoof.AdUrl,
            OperatorId = spoof.OperatorId,
            IsImpersonating = spoof.IsImpersonating,
            OriginalPrice = spoof.OriginalPrice,
            PromotionalPrice = spoof.PromotionalPrice,
            CreatedAt = spoof.CreatedAt,
            UpdatedAt = spoof.UpdatedAt,
        };

    public static OperatorAdSpoofDetails ToOperatorDetails(AdSpoof spoof) =>
        new()
        {
            Id = spoof.Id,
            OperationId = spoof.OperationId,
            AdId = spoof.AdId,
            AdUrl = spoof.AdUrl,
            IsImpersonating = spoof.IsImpersonating,
            OriginalPrice = spoof.OriginalPrice,
            PromotionalPrice = spoof.PromotionalPrice,
            CreatedAt = spoof.CreatedAt,
            UpdatedAt = spoof.UpdatedAt,
        };
}

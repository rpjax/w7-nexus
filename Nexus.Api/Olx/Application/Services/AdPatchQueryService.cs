using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Services;

public sealed class AdPatchQueryService : IAdPatchQueryService
{
    private readonly IAdPatchRepository _adPatches;

    public AdPatchQueryService(IAdPatchRepository adPatches)
    {
        _adPatches = adPatches;
    }

    public async Task<IResult<ListPatchedAdsResponse>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _adPatches.AsQueryable()
            .Where(s => s.OriginalPrice != null || s.PromotionalPrice != null)
            .OrderByDescending(s => s.UpdatedAt)
            .ToArrayAsync();

        return Result<ListPatchedAdsResponse>.Success(new ListPatchedAdsResponse
        {
            Items = items.Select(ToVictimDetails).ToArray(),
        });
    }

    public async Task<AdPatch?> FindByOperationAndAdAsync(
        string operationId,
        string adId,
        CancellationToken cancellationToken = default)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        adId = adId?.Trim() ?? string.Empty;

        var patches = await _adPatches.AsQueryable()
            .Where(s => s.OperationId == operationId && s.AdId == adId)
            .ToArrayAsync();

        return patches.FirstOrDefault();
    }

    internal static PatchedAdDetails ToVictimDetails(AdPatch patch) =>
        new()
        {
            AdId = patch.AdId,
            OriginalPrice = patch.OriginalPrice,
            PromotionalPrice = patch.PromotionalPrice,
        };
}

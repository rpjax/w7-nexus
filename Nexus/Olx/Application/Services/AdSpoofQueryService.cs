using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Olx.Aggregates;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Services;

public sealed class AdSpoofQueryService : IAdSpoofQueryService
{
    private readonly IAdSpoofRepository _adSpoofs;

    public AdSpoofQueryService(IAdSpoofRepository adSpoofs)
    {
        _adSpoofs = adSpoofs;
    }

    public async Task<IResult<ListSpoofedAdsResponse>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _adSpoofs.AsQueryable()
            .Where(s => s.OriginalPrice != null || s.PromotionalPrice != null)
            .OrderByDescending(s => s.UpdatedAt)
            .ToArrayAsync();

        return Result<ListSpoofedAdsResponse>.Success(new ListSpoofedAdsResponse
        {
            Items = items.Select(ToVictimDetails).ToArray(),
        });
    }

    internal static SpoofedAdDetails ToVictimDetails(AdSpoof spoof) =>
        new()
        {
            AdId = spoof.AdId,
            OriginalPrice = spoof.OriginalPrice,
            PromotionalPrice = spoof.PromotionalPrice,
        };
}

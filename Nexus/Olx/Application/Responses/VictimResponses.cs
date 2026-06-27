namespace Nexus.Olx.Application.Responses;

public sealed class SpoofedAdDetails
{
    public string AdId { get; init; } = string.Empty;
    public decimal? OriginalPrice { get; init; }
    public decimal? PromotionalPrice { get; init; }
}

public sealed class ListSpoofedAdsResponse
{
    public IReadOnlyList<SpoofedAdDetails> Items { get; init; } = Array.Empty<SpoofedAdDetails>();
}

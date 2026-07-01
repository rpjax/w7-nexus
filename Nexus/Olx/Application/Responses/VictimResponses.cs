namespace Nexus.Olx.Application.Responses;

public sealed class PatchedAdDetails
{
    public string AdId { get; init; } = string.Empty;
    public decimal? OriginalPrice { get; init; }
    public decimal? PromotionalPrice { get; init; }
}

public sealed class ListPatchedAdsResponse
{
    public IReadOnlyList<PatchedAdDetails> Items { get; init; } = Array.Empty<PatchedAdDetails>();
}

public sealed class CreatePixPaymentResponse
{
    public string PixCode { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public int ExpirationTimeSeconds { get; init; }
    public string PaymentRecipient { get; init; } = string.Empty;
}

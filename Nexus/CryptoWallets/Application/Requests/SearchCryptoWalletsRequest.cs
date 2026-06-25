namespace Nexus.CryptoWallets.Application.Requests;

public sealed class SearchCryptoWalletsRequest
{
    public string? OwnerId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

namespace Nexus.CryptoWallets.Application.Contracts;

public sealed class SearchCryptoWalletsRequest
{
    public string? StrawManId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public sealed class SearchCryptoWalletsResponse
{
    public int Total { get; init; }
    public IReadOnlyList<CryptoWallet> Items { get; init; } = Array.Empty<CryptoWallet>();
}

using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public sealed class UpsertCryptoWalletAddressBody
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

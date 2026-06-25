using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public sealed class UpsertCryptoWalletAddressRequest
{
    public string CryptoWalletId { get; init; } = string.Empty;
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Requests;

public sealed class CreateCryptoWalletAddressRequest
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

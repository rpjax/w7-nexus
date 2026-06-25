namespace Nexus.CryptoWallets.Application.Contracts;

public sealed class CreateCryptoWalletRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public IReadOnlyList<CreateCryptoWalletAddressRequest> Addresses { get; init; } = Array.Empty<CreateCryptoWalletAddressRequest>();
    public string? Label { get; init; }
}

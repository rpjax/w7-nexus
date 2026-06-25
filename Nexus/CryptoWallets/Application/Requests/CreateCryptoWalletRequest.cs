namespace Nexus.CryptoWallets.Application.Requests;

public sealed class CreateCryptoWalletRequest
{
    public string OwnerId { get; init; } = string.Empty;
    public IReadOnlyList<CreateCryptoWalletAddressRequest> Addresses { get; init; } = Array.Empty<CreateCryptoWalletAddressRequest>();
    public string? Label { get; init; }
}

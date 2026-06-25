using Nexus.CryptoWallets.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Requests;

public sealed class WithdrawalTransferRequest
{
    public IReadOnlyList<string> PaymentIds { get; init; } = Array.Empty<string>();
    public string? DestinationBankAccountId { get; init; }
    public string? DestinationCryptoWalletId { get; init; }
    public OnrampingMethod? OnrampingMethod { get; init; }
    public decimal? ProducedAmount { get; init; }
    public CryptoAsset? ProducedAsset { get; init; }
    public Chain? ProducedChain { get; init; }
    public TransferProofRequest? Proof { get; init; }
}

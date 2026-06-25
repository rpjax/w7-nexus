using Nexus.CryptoWallets.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Requests;

public sealed class BankAccountMovementRequest
{
    public string SourceBalanceId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? DestinationBankAccountId { get; init; }
    public string? DestinationCryptoWalletId { get; init; }
    public OnrampingMethod? OnrampingMethod { get; init; }
    public decimal? ProducedAmount { get; init; }
    public CryptoAsset? ProducedAsset { get; init; }
    public Chain? ProducedChain { get; init; }
    public TransferProofRequest? Proof { get; init; }
}

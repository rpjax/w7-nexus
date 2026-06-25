namespace Nexus.Transfers.Application.Requests;

public sealed class PayoutTransferRequest
{
    public string SourceBalanceId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? DestinationBankAccountId { get; init; }
    public string? DestinationCryptoWalletId { get; init; }
    public TransferProofRequest Proof { get; init; } = new();
}

namespace Nexus.Transfers.Application.Requests;

public sealed class TransferProofRequest
{
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

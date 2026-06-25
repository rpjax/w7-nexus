using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Presentation;

public static class TransferApiMapping
{
    public static object ToTransferResponse(Transfer transfer) => new
    {
        id = transfer.Id,
        type = transfer.Type.ToString(),
        onrampingMethod = transfer.OnrampingMethod?.ToString(),
        proof = transfer.Proof is null
            ? null
            : new
            {
                pixTransactionId = transfer.Proof.PixTransactionId,
                pixAuthenticationCode = transfer.Proof.PixAuthenticationCode,
                cryptoTransactionId = transfer.Proof.CryptoTransactionId,
            },
        originType = transfer.OriginType?.ToString(),
        originBankAccount = ToOriginBankAccount(transfer.OriginBankAccount),
        originCryptoWallet = ToOriginCryptoWallet(transfer.OriginCryptoWallet),
        destinationType = transfer.DestinationType?.ToString(),
        destinationBankAccount = ToDestinationBankAccount(transfer.DestinationBankAccount),
        destinationCryptoWallet = ToDestinationCryptoWallet(transfer.DestinationCryptoWallet),
        sourceAmount = transfer.SourceAmount,
        producedAmount = transfer.ProducedAmount,
        producedAsset = transfer.ProducedAsset?.ToString(),
        producedChain = transfer.ProducedChain?.ToString(),
        paymentIds = transfer.PaymentIds,
        sourceBalanceId = transfer.SourceBalanceId,
        strawManId = transfer.StrawManId,
        createdAt = transfer.CreatedAt,
    };

    private static object? ToOriginBankAccount(TransferOriginBankAccount? endpoint) =>
        endpoint is null ? null : new { bankAccountId = endpoint.BankAccountId, ownerId = endpoint.OwnerId };

    private static object? ToOriginCryptoWallet(TransferOriginCryptoWallet? endpoint) =>
        endpoint is null ? null : new { cryptoWalletId = endpoint.CryptoWalletId, ownerId = endpoint.OwnerId };

    private static object? ToDestinationBankAccount(TransferDestinationBankAccount? endpoint) =>
        endpoint is null ? null : new { bankAccountId = endpoint.BankAccountId, ownerId = endpoint.OwnerId };

    private static object? ToDestinationCryptoWallet(TransferDestinationCryptoWallet? endpoint) =>
        endpoint is null ? null : new { cryptoWalletId = endpoint.CryptoWalletId, ownerId = endpoint.OwnerId };
}

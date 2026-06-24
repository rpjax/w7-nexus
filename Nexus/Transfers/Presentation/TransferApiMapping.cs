using Nexus.AccountNodes.Aggregates;
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
        source = ToNodeSnapshot(transfer.Source),
        destination = ToNodeSnapshot(transfer.Destination),
        sourceAmount = transfer.SourceAmount,
        producedAmount = transfer.ProducedAmount,
        producedAsset = transfer.ProducedAsset?.ToString(),
        producedChain = transfer.ProducedChain?.ToString(),
        paymentIds = transfer.PaymentIds,
        sourceBalanceId = transfer.SourceBalanceId,
        strawManId = transfer.StrawManId,
        createdAt = transfer.CreatedAt,
    };

    private static object? ToNodeSnapshot(AccountNodeSnapshot? snapshot)
    {
        if (snapshot is null)
            return null;

        return new
        {
            kind = snapshot.Kind.ToString(),
            bankAccountId = snapshot.BankAccountId,
            cryptoWalletId = snapshot.CryptoWalletId,
            participantAccountId = snapshot.ParticipantAccountId,
            strawManId = snapshot.StrawManId,
        };
    }
}

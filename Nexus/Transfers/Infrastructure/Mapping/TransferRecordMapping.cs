using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Infrastructure.Mapping;

internal static class TransferRecordMapping
{
    public static Transfer ToTransfer(TransferRecord record) =>
        new(
            record.Id.ToString(),
            record.Type,
            record.OnrampingMethod,
            MapProof(record.Proof),
            record.OriginType,
            MapOriginBankAccount(record.OriginBankAccount),
            MapOriginCryptoWallet(record.OriginCryptoWallet),
            record.DestinationType,
            MapDestinationBankAccount(record.DestinationBankAccount),
            MapDestinationCryptoWallet(record.DestinationCryptoWallet),
            record.SourceAmount,
            record.ProducedAmount,
            record.ProducedAsset,
            record.ProducedChain,
            record.PaymentIds,
            record.SourceBalanceId,
            record.StrawManId,
            record.CreatedAt);

    public static TransferRecord ToRecord(Transfer entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            Type = entity.Type,
            OnrampingMethod = entity.OnrampingMethod,
            Proof = MapProof(entity.Proof),
            OriginType = entity.OriginType,
            OriginBankAccount = MapOriginBankAccount(entity.OriginBankAccount),
            OriginCryptoWallet = MapOriginCryptoWallet(entity.OriginCryptoWallet),
            DestinationType = entity.DestinationType,
            DestinationBankAccount = MapDestinationBankAccount(entity.DestinationBankAccount),
            DestinationCryptoWallet = MapDestinationCryptoWallet(entity.DestinationCryptoWallet),
            SourceAmount = entity.SourceAmount,
            ProducedAmount = entity.ProducedAmount,
            ProducedAsset = entity.ProducedAsset,
            ProducedChain = entity.ProducedChain,
            PaymentIds = entity.PaymentIds.ToList(),
            SourceBalanceId = entity.SourceBalanceId,
            StrawManId = entity.StrawManId,
            CreatedAt = entity.CreatedAt,
        };

    private static TransferProof? MapProof(TransferProofRecord? record)
    {
        if (record is null)
            return null;

        if (record.PixTransactionId is null
            && record.PixAuthenticationCode is null
            && record.CryptoTransactionId is null)
            return null;

        return new TransferProof(record.PixTransactionId, record.PixAuthenticationCode, record.CryptoTransactionId);
    }

    private static TransferProofRecord? MapProof(TransferProof? proof)
    {
        if (proof is null)
            return null;

        if (proof.PixTransactionId is null
            && proof.PixAuthenticationCode is null
            && proof.CryptoTransactionId is null)
            return null;

        return new TransferProofRecord
        {
            PixTransactionId = proof.PixTransactionId,
            PixAuthenticationCode = proof.PixAuthenticationCode,
            CryptoTransactionId = proof.CryptoTransactionId,
        };
    }

    private static TransferOriginBankAccount? MapOriginBankAccount(TransferOriginBankAccountRecord? record) =>
        record is null ? null : new TransferOriginBankAccount(record.BankAccountId, record.OwnerId);

    private static TransferOriginBankAccountRecord? MapOriginBankAccount(TransferOriginBankAccount? endpoint) =>
        endpoint is null ? null : new TransferOriginBankAccountRecord
        {
            BankAccountId = endpoint.BankAccountId,
            OwnerId = endpoint.OwnerId,
        };

    private static TransferOriginCryptoWallet? MapOriginCryptoWallet(TransferOriginCryptoWalletRecord? record) =>
        record is null ? null : new TransferOriginCryptoWallet(record.CryptoWalletId, record.OwnerId);

    private static TransferOriginCryptoWalletRecord? MapOriginCryptoWallet(TransferOriginCryptoWallet? endpoint) =>
        endpoint is null ? null : new TransferOriginCryptoWalletRecord
        {
            CryptoWalletId = endpoint.CryptoWalletId,
            OwnerId = endpoint.OwnerId,
        };

    private static TransferDestinationBankAccount? MapDestinationBankAccount(TransferDestinationBankAccountRecord? record) =>
        record is null ? null : new TransferDestinationBankAccount(record.BankAccountId, record.OwnerId);

    private static TransferDestinationBankAccountRecord? MapDestinationBankAccount(TransferDestinationBankAccount? endpoint) =>
        endpoint is null ? null : new TransferDestinationBankAccountRecord
        {
            BankAccountId = endpoint.BankAccountId,
            OwnerId = endpoint.OwnerId,
        };

    private static TransferDestinationCryptoWallet? MapDestinationCryptoWallet(TransferDestinationCryptoWalletRecord? record) =>
        record is null ? null : new TransferDestinationCryptoWallet(record.CryptoWalletId, record.OwnerId);

    private static TransferDestinationCryptoWalletRecord? MapDestinationCryptoWallet(TransferDestinationCryptoWallet? endpoint) =>
        endpoint is null ? null : new TransferDestinationCryptoWalletRecord
        {
            CryptoWalletId = endpoint.CryptoWalletId,
            OwnerId = endpoint.OwnerId,
        };
}

using MongoDB.Bson;
using Nexus.AccountNodes.Aggregates;
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
            MapSnapshot(record.Source),
            MapSnapshot(record.Destination),
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
            Source = MapSnapshot(entity.Source),
            Destination = MapSnapshot(entity.Destination),
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

    private static AccountNodeSnapshot? MapSnapshot(AccountNodeSnapshotRecord? record)
    {
        if (record is null)
            return null;

        return new AccountNodeSnapshot(
            record.Kind,
            record.BankAccountId,
            record.CryptoWalletId,
            record.ParticipantAccountId,
            record.StrawManId);
    }

    private static AccountNodeSnapshotRecord? MapSnapshot(AccountNodeSnapshot? snapshot)
    {
        if (snapshot is null)
            return null;

        return new AccountNodeSnapshotRecord
        {
            Kind = snapshot.Kind,
            BankAccountId = snapshot.BankAccountId,
            CryptoWalletId = snapshot.CryptoWalletId,
            ParticipantAccountId = snapshot.ParticipantAccountId,
            StrawManId = snapshot.StrawManId,
        };
    }
}

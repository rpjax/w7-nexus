using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Withdrawals.Aggregates;

namespace Nexus.Withdrawals.Infrastructure.Mapping;

internal static class BankAccountRecordMapping
{
    public static BankAccount ToBankAccount(BankAccountRecord record) =>
        new(
            record.Id.ToString(),
            record.StrawManAccountId,
            record.Bank,
            record.Agency,
            record.AccountNumber,
            record.AccountDigit,
            record.AccountType,
            record.PixKey,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt);

    public static BankAccountRecord ToRecord(BankAccount entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            StrawManAccountId = entity.StrawManAccountId,
            Bank = entity.Bank,
            Agency = entity.Agency,
            AccountNumber = entity.AccountNumber,
            AccountDigit = entity.AccountDigit,
            AccountType = entity.AccountType,
            PixKey = entity.PixKey,
            Label = entity.Label,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
}

internal static class CryptoWalletRecordMapping
{
    public static CryptoWallet ToCryptoWallet(CryptoWalletRecord record) =>
        new(
            record.Id.ToString(),
            record.StrawManAccountId,
            record.Chain,
            record.Asset,
            record.Address,
            record.Memo,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt);

    public static CryptoWalletRecord ToRecord(CryptoWallet entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            StrawManAccountId = entity.StrawManAccountId,
            Chain = entity.Chain,
            Asset = entity.Asset,
            Address = entity.Address,
            Memo = entity.Memo,
            Label = entity.Label,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
}

internal static class WithdrawalRecordMapping
{
    public static Withdrawal ToWithdrawal(WithdrawalRecord record) =>
        new(
            record.Id.ToString(),
            record.OperationId,
            record.Type,
            record.StrawManAccountId,
            record.BankAccountId,
            record.CryptoWalletId,
            record.PaymentIds,
            record.CostDescription,
            record.CostAmount,
            MapPixProof(record.PixProof),
            MapCryptoProof(record.CryptoProof),
            record.PaymentsTotalAmount,
            record.NetAmount,
            record.CreatedAt);

    public static WithdrawalRecord ToRecord(Withdrawal entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            OperationId = entity.OperationId,
            Type = entity.Type,
            StrawManAccountId = entity.StrawManAccountId,
            BankAccountId = entity.BankAccountId,
            CryptoWalletId = entity.CryptoWalletId,
            PaymentIds = entity.PaymentIds.ToList(),
            CostDescription = entity.CostDescription,
            CostAmount = entity.CostAmount,
            PixProof = MapPixProof(entity.PixProof),
            CryptoProof = MapCryptoProof(entity.CryptoProof),
            PaymentsTotalAmount = entity.PaymentsTotalAmount,
            NetAmount = entity.NetAmount,
            CreatedAt = entity.CreatedAt,
        };

    private static PixProof? MapPixProof(PixProofRecord? record)
    {
        if (record is null)
            return null;

        if (record.TransactionId is null && record.AuthenticationCode is null)
            return null;

        return new PixProof(record.TransactionId, record.AuthenticationCode);
    }

    private static PixProofRecord? MapPixProof(PixProof? proof)
    {
        if (proof is null)
            return null;

        if (proof.TransactionId is null && proof.AuthenticationCode is null)
            return null;

        return new PixProofRecord
        {
            TransactionId = proof.TransactionId,
            AuthenticationCode = proof.AuthenticationCode,
        };
    }

    private static CryptoProof? MapCryptoProof(CryptoProofRecord? record)
    {
        if (record is null || record.TransactionId is null)
            return null;

        return new CryptoProof(record.TransactionId);
    }

    private static CryptoProofRecord? MapCryptoProof(CryptoProof? proof)
    {
        if (proof is null || proof.TransactionId is null)
            return null;

        return new CryptoProofRecord { TransactionId = proof.TransactionId };
    }
}

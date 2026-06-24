using MongoDB.Bson;
using Nexus.AccountNodes.Aggregates;
using Nexus.Database.Models;

namespace Nexus.AccountNodes.Infrastructure.Mapping;

internal static class AccountNodeRecordMapping
{
    public static BankAccount ToBankAccount(AccountNodeBankAccountRecord record)
    {
        var balances = record.Balances.Select(ToBankBalance).ToList();
        return new BankAccount(
            record.Id.ToString(),
            record.StrawManId,
            record.Bank,
            record.Agency,
            record.AccountNumber,
            record.AccountDigit,
            record.AccountType,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt,
            balances);
    }

    public static AccountNodeBankAccountRecord ToRecord(BankAccount entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            StrawManId = entity.StrawManId,
            Bank = entity.Bank,
            Agency = entity.Agency,
            AccountNumber = entity.AccountNumber,
            AccountDigit = entity.AccountDigit,
            AccountType = entity.AccountType,
            Label = entity.Label,
            Balances = entity.Balances.Select(ToRecord).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

    public static CryptoWallet ToCryptoWallet(AccountNodeCryptoWalletRecord record) =>
        new CryptoWallet(
            record.Id.ToString(),
            record.StrawManId,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt,
            record.Addresses.Select(ToAddress).ToList(),
            record.Balances.Select(ToCryptoBalance).ToList());

    public static AccountNodeCryptoWalletRecord ToRecord(CryptoWallet entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            StrawManId = entity.StrawManId,
            Addresses = entity.Addresses.Select(ToRecord).ToList(),
            Label = entity.Label,
            Balances = entity.Balances.Select(ToRecord).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

    private static BankBalance ToBankBalance(BankBalanceRecord record) =>
        new(
            record.Id,
            record.AmountBrl,
            record.TransferId,
            record.CreatedAt,
            record.SplitSnapshot.Select(ToSplitSnapshot).ToList(),
            record.AppliedStrawManFeeIds,
            ToOriginSnapshot(record.OriginSnapshot));

    private static CryptoBalance ToCryptoBalance(CryptoBalanceRecord record) =>
        new(
            record.Id,
            record.Chain,
            record.Asset,
            record.Amount,
            record.TransferId,
            record.CreatedAt,
            record.SplitSnapshot.Select(ToSplitSnapshot).ToList(),
            record.AppliedStrawManFeeIds,
            ToOriginSnapshot(record.OriginSnapshot));

    private static CryptoWalletAddress ToAddress(CryptoWalletAddressRecord record) =>
        CryptoWalletAddress.Create(record.Namespace, record.Address, record.Memo).Value!;

    private static BalanceSplitSnapshot ToSplitSnapshot(BalanceSplitSnapshotRecord record) =>
        new(record.AccountId, record.Percentage, record.Amount, record.SplitKind);

    private static BalanceOriginSnapshot ToOriginSnapshot(BalanceOriginSnapshotRecord record) =>
        new(record.OperationId, record.OperatorId, record.StrawManId);

    private static BankBalanceRecord ToRecord(BankBalance balance) =>
        new()
        {
            Id = balance.Id,
            AmountBrl = balance.AmountBrl,
            TransferId = balance.TransferId,
            CreatedAt = balance.CreatedAt,
            SplitSnapshot = balance.SplitSnapshot.Select(ToRecord).ToList(),
            AppliedStrawManFeeIds = balance.AppliedStrawManFeeIds.ToList(),
            OriginSnapshot = ToRecord(balance.OriginSnapshot),
        };

    private static CryptoBalanceRecord ToRecord(CryptoBalance balance) =>
        new()
        {
            Id = balance.Id,
            Chain = balance.Chain,
            Asset = balance.Asset,
            Amount = balance.Amount,
            TransferId = balance.TransferId,
            CreatedAt = balance.CreatedAt,
            SplitSnapshot = balance.SplitSnapshot.Select(ToRecord).ToList(),
            AppliedStrawManFeeIds = balance.AppliedStrawManFeeIds.ToList(),
            OriginSnapshot = ToRecord(balance.OriginSnapshot),
        };

    private static CryptoWalletAddressRecord ToRecord(CryptoWalletAddress address) =>
        new()
        {
            Namespace = address.Namespace,
            Address = address.Address,
            Memo = address.Memo,
        };

    private static BalanceSplitSnapshotRecord ToRecord(BalanceSplitSnapshot snapshot) =>
        new()
        {
            AccountId = snapshot.AccountId,
            Percentage = snapshot.Percentage,
            Amount = snapshot.Amount,
            SplitKind = snapshot.SplitKind,
        };

    private static BalanceOriginSnapshotRecord ToRecord(BalanceOriginSnapshot snapshot) =>
        new()
        {
            OperationId = snapshot.OperationId,
            OperatorId = snapshot.OperatorId,
            StrawManId = snapshot.StrawManId,
        };
}

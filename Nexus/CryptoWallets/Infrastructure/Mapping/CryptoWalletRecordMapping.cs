using MongoDB.Bson;
using Nexus.CryptoWallets.Aggregates;
using Nexus.Database.Models;

namespace Nexus.CryptoWallets.Infrastructure.Mapping;

internal static class CryptoWalletRecordMapping
{
    public static CryptoWallet ToCryptoWallet(CryptoWalletRecord record) =>
        new(
            record.Id.ToString(),
            record.StrawManId,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt,
            record.Addresses.Select(ToAddress).ToList(),
            record.Balances.Select(ToCryptoBalance).ToList());

    public static CryptoWalletRecord ToRecord(CryptoWallet entity) =>
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

    private static CryptoBalance ToCryptoBalance(CryptoBalanceRecord record) =>
        new(
            record.Id,
            record.Chain,
            record.Asset,
            record.Amount,
            record.TransferId,
            record.CreatedAt,
            record.Splits.Select(ToSplit).ToList(),
            record.AppliedStrawManFeeIds,
            ToOrigin(record.Origin));

    private static CryptoWalletAddress ToAddress(CryptoWalletAddressRecord record) =>
        CryptoWalletAddress.Create(record.Namespace, record.Address, record.Memo).Value!;

    private static CryptoBalanceSplit ToSplit(CryptoBalanceSplitRecord record) =>
        new(record.AccountId, record.Percentage, record.Amount, record.SplitKind);

    private static CryptoBalanceOrigin ToOrigin(CryptoBalanceOriginRecord record) =>
        new(record.OperationId, record.OperatorId, record.StrawManId);

    private static CryptoBalanceRecord ToRecord(CryptoBalance balance) =>
        new()
        {
            Id = balance.Id,
            Chain = balance.Chain,
            Asset = balance.Asset,
            Amount = balance.Amount,
            TransferId = balance.TransferId,
            CreatedAt = balance.CreatedAt,
            Splits = balance.Splits.Select(ToRecord).ToList(),
            AppliedStrawManFeeIds = balance.AppliedStrawManFeeIds.ToList(),
            Origin = ToRecord(balance.Origin),
        };

    private static CryptoWalletAddressRecord ToRecord(CryptoWalletAddress address) =>
        new()
        {
            Namespace = address.Namespace,
            Address = address.Address,
            Memo = address.Memo,
        };

    private static CryptoBalanceSplitRecord ToRecord(CryptoBalanceSplit split) =>
        new()
        {
            AccountId = split.AccountId,
            Percentage = split.Percentage,
            Amount = split.Amount,
            SplitKind = split.SplitKind,
        };

    private static CryptoBalanceOriginRecord ToRecord(CryptoBalanceOrigin origin) =>
        new()
        {
            OperationId = origin.OperationId,
            OperatorId = origin.OperatorId,
            StrawManId = origin.StrawManId,
        };
}

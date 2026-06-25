using Nexus.CryptoWallets.Aggregates;
using Nexus.Database.Models;

namespace Nexus.CryptoWallets.Infrastructure.Mapping;

internal static class CryptoBalanceDocumentMapping
{
    public static CryptoBalance ToCryptoBalance(CryptoBalanceDocument document) =>
        new(
            document.Id,
            document.CryptoWalletId,
            document.Chain,
            document.Asset,
            document.Amount,
            document.TransferId,
            document.CreatedAt,
            document.Splits.Select(ToSplit).ToList(),
            ToOrigin(document.Origin));

    public static CryptoBalanceDocument ToDocument(CryptoBalance entity) =>
        new()
        {
            Id = entity.Id,
            CryptoWalletId = entity.CryptoWalletId,
            Chain = entity.Chain,
            Asset = entity.Asset,
            Amount = entity.Amount,
            TransferId = entity.TransferId,
            CreatedAt = entity.CreatedAt,
            Splits = entity.Splits.Select(ToRecord).ToList(),
            Origin = ToRecord(entity.Origin),
        };

    private static CryptoBalanceSplit ToSplit(CryptoBalanceSplitRecord record) =>
        new(record.AccountId, record.Percentage, record.Amount, record.SplitKind);

    private static CryptoBalanceOrigin ToOrigin(CryptoBalanceOriginRecord record) =>
        new(record.OperationId, record.OperatorId);

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
        };
}

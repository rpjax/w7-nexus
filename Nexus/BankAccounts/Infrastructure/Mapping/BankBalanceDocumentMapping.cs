using Nexus.BankAccounts.Aggregates;
using Nexus.Database.Models;

namespace Nexus.BankAccounts.Infrastructure.Mapping;

internal static class BankBalanceDocumentMapping
{
    public static BankBalance ToBankBalance(BankBalanceDocument document) =>
        new(
            document.Id,
            document.BankAccountId,
            document.AmountBrl,
            document.TransferId,
            document.CreatedAt,
            document.Splits.Select(ToSplit).ToList(),
            ToOrigin(document.Origin));

    public static BankBalanceDocument ToDocument(BankBalance entity) =>
        new()
        {
            Id = entity.Id,
            BankAccountId = entity.BankAccountId,
            AmountBrl = entity.AmountBrl,
            TransferId = entity.TransferId,
            CreatedAt = entity.CreatedAt,
            Splits = entity.Splits.Select(ToRecord).ToList(),
            Origin = ToRecord(entity.Origin),
        };

    private static BankBalanceSplit ToSplit(BankBalanceSplitRecord record) =>
        new(record.AccountId, record.Percentage, record.Amount, record.SplitKind);

    private static BankBalanceOrigin ToOrigin(BankBalanceOriginRecord record) =>
        new(record.OperationId, record.OperatorId);

    private static BankBalanceSplitRecord ToRecord(BankBalanceSplit split) =>
        new()
        {
            AccountId = split.AccountId,
            Percentage = split.Percentage,
            Amount = split.Amount,
            SplitKind = split.SplitKind,
        };

    private static BankBalanceOriginRecord ToRecord(BankBalanceOrigin origin) =>
        new()
        {
            OperationId = origin.OperationId,
            OperatorId = origin.OperatorId,
        };
}

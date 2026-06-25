using MongoDB.Bson;
using Nexus.BankAccounts.Aggregates;
using Nexus.Database.Models;

namespace Nexus.BankAccounts.Infrastructure.Mapping;

internal static class BankAccountRecordMapping
{
    public static BankAccount ToBankAccount(BankAccountRecord record)
    {
        var balances = record.Balances.Select(ToBankBalance).ToList();
        return new BankAccount(
            record.Id.ToString(),
            record.OwnerId,
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

    public static BankAccountRecord ToRecord(BankAccount entity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(entity.Id),
            OwnerId = entity.OwnerId,
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

    private static BankBalance ToBankBalance(BankBalanceRecord record) =>
        new(
            record.Id,
            record.AmountBrl,
            record.TransferId,
            record.CreatedAt,
            record.Splits.Select(ToSplit).ToList(),
            record.AppliedStrawManFeeIds,
            ToOrigin(record.Origin));

    private static BankBalanceSplit ToSplit(BankBalanceSplitRecord record) =>
        new(record.AccountId, record.Percentage, record.Amount, record.SplitKind);

    private static BankBalanceOrigin ToOrigin(BankBalanceOriginRecord record) =>
        new(record.OperationId, record.OperatorId, record.StrawManId);

    private static BankBalanceRecord ToRecord(BankBalance balance) =>
        new()
        {
            Id = balance.Id,
            AmountBrl = balance.AmountBrl,
            TransferId = balance.TransferId,
            CreatedAt = balance.CreatedAt,
            Splits = balance.Splits.Select(ToRecord).ToList(),
            AppliedStrawManFeeIds = balance.AppliedStrawManFeeIds.ToList(),
            Origin = ToRecord(balance.Origin),
        };

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
            StrawManId = origin.StrawManId,
        };
}

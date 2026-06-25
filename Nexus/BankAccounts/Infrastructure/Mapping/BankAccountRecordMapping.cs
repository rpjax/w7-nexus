using MongoDB.Bson;
using Nexus.BankAccounts.Aggregates;
using Nexus.Database.Models;

namespace Nexus.BankAccounts.Infrastructure.Mapping;

internal static class BankAccountRecordMapping
{
    public static BankAccount ToBankAccount(BankAccountRecord record) =>
        new(
            record.Id.ToString(),
            record.OwnerId,
            record.Bank,
            record.Agency,
            record.AccountNumber,
            record.AccountDigit,
            record.AccountType,
            record.Label,
            record.CreatedAt,
            record.UpdatedAt);

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
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
}

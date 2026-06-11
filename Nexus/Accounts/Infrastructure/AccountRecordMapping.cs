using MongoDB.Bson;
using Nexus.Accounts.Aggregates;
using Nexus.Database.Models;

namespace Nexus.Accounts.Infrastructure;

internal static class AccountRecordMapping
{
    public static Account ToAccount(AccountRecord record)
    {
        return new Account(
            record.Id.ToString(),
            record.Username,
            record.PasswordHash,
            record.Roles,
            record.Permissions,
            record.CreatedAt,
            record.LastUpdatedAt);
    }

    public static AccountRecord ToRecord(Account account)
    {
        if (!ObjectId.TryParse(account.Id, out var objectId))
            objectId = ObjectId.GenerateNewId();

        return new AccountRecord
        {
            Id = objectId,
            Username = account.Username,
            PasswordHash = account.PasswordHash,
            Roles = account.Roles.ToList(),
            Permissions = account.Permissions.ToList(),
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };
    }
}

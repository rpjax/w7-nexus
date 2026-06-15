using Nexus.Accounts.Aggregates;
using Nexus.Actors.Responses.Models;

namespace Nexus.Actors.Extensions;

public static class AccountExtensions
{
    public static AccountDetails ToAccountDetails(this Account account)
    {
        return new AccountDetails
        {
            Id = account.Id,
            Username = account.Username,
            Roles = account.Roles.ToArray(),
            Permissions = account.Permissions.ToArray(),
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt,
        };
    }
}

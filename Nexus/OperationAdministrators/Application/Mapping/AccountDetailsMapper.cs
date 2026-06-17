using Nexus.Accounts.Aggregates;
using Nexus.OperationAdministrators.Application.Responses.Models;

namespace Nexus.OperationAdministrators.Application.Mapping;

public static class AccountDetailsMapper
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

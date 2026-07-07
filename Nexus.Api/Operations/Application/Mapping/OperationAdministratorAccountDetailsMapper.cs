using Nexus.Accounts.Aggregates;
using Nexus.Operations.Application.Responses.OperationAdministrator.Models;

namespace Nexus.Operations.Application.Mapping;

public static class OperationAdministratorAccountDetailsMapper
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

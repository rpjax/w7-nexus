using Nexus.Accounts.Aggregates;
using Nexus.Operations.Application.Responses.TeamLeader.Models;

namespace Nexus.Operations.Application.Mapping;

public static class TeamLeaderAccountDetailsMapper
{
    public static AccountDetails ToTeamLeaderAccountDetails(this Account account)
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

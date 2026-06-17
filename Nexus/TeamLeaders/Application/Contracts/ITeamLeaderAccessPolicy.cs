using Nexus.Authorizations.Application.Models;

namespace Nexus.TeamLeaders.Application.Contracts;

public interface ITeamLeaderAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeSearchLedTeamsAsync(RequesterIdentity identity);

    Task<IAuthorizationResult> AuthorizeManageTeamAsync(RequesterIdentity identity, string teamId);
}

using Nexus.Authorization.Application.Models;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamLeaderAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeSearchLedTeamsAsync(RequesterIdentity identity);

    Task<IAuthorizationResult> AuthorizeManageTeamAsync(RequesterIdentity identity, string teamId);
}

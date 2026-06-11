using Nexus.Actors.Contracts;
using Nexus.Authorization.Results;

namespace Nexus.Authorization.Services.Contracts;

public interface ITeamLeaderAccess
{
    Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

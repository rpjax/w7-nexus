using Nexus.Actors.Contracts;

namespace Nexus.Authorization;

public interface ITeamLeaderAccess
{
    Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

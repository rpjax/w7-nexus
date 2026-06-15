using Nexus.Authorization.Application.Models;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeaderAccess
{
    Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

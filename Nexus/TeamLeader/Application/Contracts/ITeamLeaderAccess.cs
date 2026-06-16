using Nexus.Authorization.Application.Models;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeaderAccess
{
    Task<IAccessEvaluationResult<ITeamLeader>> ResolveAsync(
        CancellationToken cancellationToken = default);

    Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

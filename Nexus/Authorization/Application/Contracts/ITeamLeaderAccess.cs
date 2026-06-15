using Nexus.Actors.Contracts;
using Nexus.Authorization.Application.Models;

namespace Nexus.Authorization.Application.Services.Contracts;

public interface ITeamLeaderAccess
{
    Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

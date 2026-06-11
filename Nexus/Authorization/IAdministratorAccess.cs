using Nexus.Actors.Contracts;

namespace Nexus.Authorization;

public interface IAdministratorAccess
{
    Task<IAccessEvaluationResult<IAdministrator>> ResolveAsync(CancellationToken cancellationToken = default);
}

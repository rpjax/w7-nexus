using Nexus.Actors.Contracts;
using Nexus.Authorization.Results;

namespace Nexus.Authorization.Services.Contracts;

public interface IAdministratorAccess
{
    Task<IAccessEvaluationResult<IAdministrator>> ResolveAsync(CancellationToken cancellationToken = default);
}

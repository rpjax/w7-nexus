using Nexus.Authorization.Application.Models;

namespace Nexus.Administrator.Application.Contracts;

public interface IAdministratorAccess
{
    Task<IAccessEvaluationResult<IAdministrator>> ResolveAsync(CancellationToken cancellationToken = default);
}

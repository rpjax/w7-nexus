using Nexus.Actors.Contracts;
using Nexus.Authorization.Application.Models;

namespace Nexus.Authorization.Application.Services.Contracts;

public interface IAdministratorAccess
{
    Task<IAccessEvaluationResult<IAdministrator>> ResolveAsync(CancellationToken cancellationToken = default);
}

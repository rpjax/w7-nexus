using Nexus.Actors.Contracts;
using Nexus.Authorization.Results;

namespace Nexus.Authorization.Services.Contracts;

public interface IOperationAdministratorAccess
{
    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

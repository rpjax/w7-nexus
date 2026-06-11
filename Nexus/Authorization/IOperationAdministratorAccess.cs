using Nexus.Actors.Contracts;

namespace Nexus.Authorization;

public interface IOperationAdministratorAccess
{
    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

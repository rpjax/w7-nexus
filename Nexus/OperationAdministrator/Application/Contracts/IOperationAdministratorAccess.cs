using Nexus.Authorization.Application.Models;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface IOperationAdministratorAccess
{
    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<IAccessEvaluationResult<IOperationAdministrator>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default);
}

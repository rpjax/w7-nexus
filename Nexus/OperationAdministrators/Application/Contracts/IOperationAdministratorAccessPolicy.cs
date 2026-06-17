using Nexus.Authorization.Application.Models;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeSearchOperationsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default);

    Task<IAuthorizationResult> AuthorizeManageOperationAsync(
        RequesterIdentity identity,
        string? operationId = null,
        string? teamId = null,
        CancellationToken cancellationToken = default);
}

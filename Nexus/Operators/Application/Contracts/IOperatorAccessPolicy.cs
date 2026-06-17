using Nexus.Authorization.Application.Models;

namespace Nexus.Operators.Application.Contracts;

public interface IOperatorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeSearchOperationsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default);
}

using Aidan.Core.Errors;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Operator.Application.Contracts;

namespace Nexus.Operator.Application.Services;

public sealed class OperatorAccessPolicy : IOperatorAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeSearchOperationsAsync(
        RequesterIdentity identity,
        CancellationToken cancellationToken = default)
    {
        if (identity.Roles.Contains(Roles.Operator, StringComparer.Ordinal))
        {
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());
        }

        return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotOperator)
            .WithMessage("Acesso de operador necessário para realizar esta ação.")
            .Build()));
    }
}

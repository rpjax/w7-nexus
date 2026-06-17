using Aidan.Core.Errors;
using Nexus.Authorizations;
using Nexus.Authorizations.Application.Models;
using Nexus.Authorizations.Errors;
using Nexus.Operators.Application.Contracts;

namespace Nexus.Operators.Application.Services;

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

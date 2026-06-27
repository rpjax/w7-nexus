using Aidan.Core.Errors;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Olx.Application.Contracts;

namespace Nexus.Olx.Application.Services;

public sealed class OlxOperatorAccessPolicy : IOlxOperatorAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeOlxOperatorAsync(RequesterIdentity identity)
    {
        if (identity.Roles.Contains(Roles.OlxOperator, StringComparer.Ordinal))
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());

        return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotOlxOperator)
            .WithMessage("Acesso de operador OLX necessário para realizar esta ação.")
            .Build()));
    }
}

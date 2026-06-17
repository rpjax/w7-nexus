using Aidan.Core.Errors;
using Nexus.Authorizations;
using Nexus.Authorizations.Application.Models;
using Nexus.Authorizations.Errors;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Application.Services;

public sealed class StrawManAccessPolicy : IStrawManAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeStrawManAsync(RequesterIdentity identity)
    {
        if (identity.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
        {
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());
        }

        return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotStrawMan)
            .WithMessage("Acesso de laranja necessário para realizar esta ação.")
            .Build()));
    }
}

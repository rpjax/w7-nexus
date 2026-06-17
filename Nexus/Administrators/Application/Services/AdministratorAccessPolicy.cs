using Aidan.Core.Errors;
using Nexus.Authorizations;
using Nexus.Authorizations.Application.Models;
using Nexus.Authorizations.Errors;
using Nexus.Administrators.Application.Contracts;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorAccessPolicy : IAdministratorAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity)
    {
        if (identity.Roles.Contains(Roles.Administrator, StringComparer.Ordinal))
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());

        return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotAdministrator)
            .WithMessage("Acesso de administrador necessário para realizar esta ação.")
            .Build()));
    }
}

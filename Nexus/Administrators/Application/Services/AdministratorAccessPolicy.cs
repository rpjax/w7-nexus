using Aidan.Core.Errors;
using Nexus.Administrators.Application.Contracts;
using Nexus.Authorization;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;

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

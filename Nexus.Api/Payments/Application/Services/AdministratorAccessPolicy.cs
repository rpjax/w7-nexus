using Aidan.Core.Errors;
using Nexus.Authorization;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Contracts;

namespace Nexus.Payments.Application.Services;

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

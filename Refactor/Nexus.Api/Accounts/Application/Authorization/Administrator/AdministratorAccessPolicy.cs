using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Authorization.Errors;

namespace Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);
}

public sealed class AdministratorAccessPolicy : IAdministratorAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (roles.Contains(Roles.Administrator, StringComparer.Ordinal))
            return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());

        return Task.FromResult<IAuthorizationResult>(AuthorizationResult.Unauthorized(Error.Create()
            .WithCode(AuthorizationErrorCodes.NotAdministrator)
            .WithMessage("Acesso de administrador necessario para realizar esta acao.")
            .Build()));
    }
}

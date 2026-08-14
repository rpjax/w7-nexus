using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Mandates.Application.Authorization;

public interface IMandateAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);
}

public sealed class MandateAccessPolicy : IMandateAccessPolicy
{
    private readonly Accounts.Application.Authorization.Administrator.IAdministratorAccessPolicy _administratorAccessPolicy;

    public MandateAccessPolicy(
        Accounts.Application.Authorization.Administrator.IAdministratorAccessPolicy administratorAccessPolicy)
    {
        _administratorAccessPolicy = administratorAccessPolicy;
    }

    public Task<IAuthorizationResult> AuthorizeAdministratorAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default) =>
        _administratorAccessPolicy.AuthorizeAsync(roles, cancellationToken);
}

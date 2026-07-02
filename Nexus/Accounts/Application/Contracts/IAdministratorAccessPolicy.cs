using Nexus.Authorization.Application.Models;

namespace Nexus.Accounts.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

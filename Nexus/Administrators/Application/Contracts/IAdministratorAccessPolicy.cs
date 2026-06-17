using Nexus.Authorization.Application.Models;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

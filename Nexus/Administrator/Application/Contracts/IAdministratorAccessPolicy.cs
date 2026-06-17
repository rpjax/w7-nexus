using Nexus.Authorization.Application.Models;

namespace Nexus.Administrator.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

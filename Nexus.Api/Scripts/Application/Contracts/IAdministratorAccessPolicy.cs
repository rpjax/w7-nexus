using Nexus.Authorization.Application.Models;

namespace Nexus.Scripts.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

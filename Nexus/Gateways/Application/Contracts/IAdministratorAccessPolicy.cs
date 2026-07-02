using Nexus.Authorization.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

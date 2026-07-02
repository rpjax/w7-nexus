using Nexus.Authorization.Application.Models;

namespace Nexus.StrawMen.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

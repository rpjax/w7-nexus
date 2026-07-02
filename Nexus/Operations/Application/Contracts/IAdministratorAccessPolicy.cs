using Nexus.Authorization.Application.Models;

namespace Nexus.Operations.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

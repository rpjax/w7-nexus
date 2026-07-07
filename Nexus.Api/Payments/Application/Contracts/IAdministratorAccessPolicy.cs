using Nexus.Authorization.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeAdministratorAsync(RequesterIdentity identity);
}

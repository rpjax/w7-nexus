using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxAdministratorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeOlxAdministratorAsync(RequesterIdentity identity);
}

using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxOperatorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeOlxOperatorAsync(RequesterIdentity identity);
}

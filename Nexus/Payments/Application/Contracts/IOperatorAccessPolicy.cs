using Nexus.Authorization.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IOperatorAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeOperatorAsync(RequesterIdentity identity);
}

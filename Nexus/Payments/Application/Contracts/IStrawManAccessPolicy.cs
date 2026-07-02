using Nexus.Authorization.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IStrawManAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeStrawManAsync(RequesterIdentity identity);
}

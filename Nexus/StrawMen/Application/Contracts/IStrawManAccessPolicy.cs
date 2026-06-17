using Nexus.Authorization.Application.Models;

namespace Nexus.StrawMen.Application.Contracts;

public interface IStrawManAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeStrawManAsync(RequesterIdentity identity);
}

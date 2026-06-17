using Nexus.Authorization.Application.Models;

namespace Nexus.StrawMan.Application.Contracts;

public interface IStrawManAccessPolicy
{
    Task<IAuthorizationResult> AuthorizeStrawManAsync(RequesterIdentity identity);
}

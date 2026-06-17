using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;

namespace Nexus.Authorization.Application.Contracts;

public interface IRequesterIdentityResolver
{
    Task<IResult<RequesterIdentity>> ResolveAsync(CancellationToken cancellationToken = default);
}

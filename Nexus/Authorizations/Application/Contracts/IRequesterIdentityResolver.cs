using Aidan.Core.Patterns;
using Nexus.Authorizations.Application.Models;

namespace Nexus.Authorizations.Application.Contracts;

public interface IRequesterIdentityResolver
{
    Task<IResult<RequesterIdentity>> ResolveAsync(CancellationToken cancellationToken = default);
}

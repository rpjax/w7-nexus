using Aidan.Core.Patterns;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application.Contracts;

public interface IGatewayCredentialsResolver
{
    Task<IResult<ResolveCredentialsResponse>> ResolveCredentialsAsync(ResolveCredentialsRequest request);
}

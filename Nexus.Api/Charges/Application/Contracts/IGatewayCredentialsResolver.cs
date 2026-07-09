using Aidan.Core.Patterns;
using Nexus.Charges.Application.Requests;
using Nexus.Charges.Application.Responses;

namespace Nexus.Charges.Application.Contracts;

public interface IGatewayCredentialsResolver
{
    Task<IResult<ResolveCredentialsResponse>> ResolveCredentialsAsync(ResolveCredentialsRequest request);
}

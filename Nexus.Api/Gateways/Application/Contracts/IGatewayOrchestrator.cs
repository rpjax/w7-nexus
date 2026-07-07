using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayOrchestrator
{
    Task<IResult<TryCreatePixResponse>> TryCreatePixAsync(TryCreatePixRequest request);
}

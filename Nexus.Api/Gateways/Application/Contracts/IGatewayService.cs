using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayService
{
    Task<CreatePixResponse> CreatePixAsync(CreatePixRequest request);
}

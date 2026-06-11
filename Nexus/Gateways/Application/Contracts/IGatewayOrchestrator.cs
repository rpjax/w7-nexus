using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayOrchestrator
{
    Task<IResult<GatewayPix>> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

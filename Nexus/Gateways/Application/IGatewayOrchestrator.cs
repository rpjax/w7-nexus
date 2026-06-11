using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application;

public interface IGatewayOrchestrator
{
    Task<IResult<GatewayPix>> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IGatewayOrchestrator
{
    Task<IResult<GatewayPix>> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

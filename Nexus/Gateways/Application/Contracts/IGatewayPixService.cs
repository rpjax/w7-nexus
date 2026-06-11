using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Contracts;

public interface IGatewayPixService
{
    Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

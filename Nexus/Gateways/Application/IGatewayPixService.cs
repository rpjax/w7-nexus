using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application;

public interface IGatewayPixService
{
    Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

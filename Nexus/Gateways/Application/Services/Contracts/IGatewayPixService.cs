using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Services.Contracts;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IGatewayPixService
{
    Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request);
}

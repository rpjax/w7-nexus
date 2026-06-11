using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Application.Services.Contracts;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IWintechGatewayPixServiceFactory
{
    IGatewayPixService Create(WintechApiCredentials credentials);
}

using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Contracts;

public interface IWintechGatewayPixServiceFactory
{
    IGatewayPixService Create(WintechApiCredentials credentials);
}

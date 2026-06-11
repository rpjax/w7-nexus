using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application;

public interface IWintechGatewayPixServiceFactory
{
    IGatewayPixService Create(WintechApiCredentials credentials);
}

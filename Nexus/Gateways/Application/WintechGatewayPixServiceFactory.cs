using Nexus.Gateways.Application;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application;

public sealed class WintechGatewayPixServiceFactory : IWintechGatewayPixServiceFactory
{
    private IWintechClient _wintechClient { get; }

    public WintechGatewayPixServiceFactory(IWintechClient wintechClient)
    {
        _wintechClient = wintechClient;
    }

    public IGatewayPixService Create(WintechApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new WintechGatewayPixService(_wintechClient, credentials);
    }
}

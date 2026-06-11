using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Wintech.Application.Services.Contracts;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Services;

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

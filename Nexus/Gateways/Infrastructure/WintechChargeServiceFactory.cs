using Nexus.Gateways.Application;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Infrastructure;

public sealed class WintechChargeServiceFactory : IWintechChargeServiceFactory
{
    private IWintechClient _wintechClient { get; }

    public WintechChargeServiceFactory(IWintechClient wintechClient)
    {
        _wintechClient = wintechClient;
    }

    public IChargeService Create(WintechApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new WintechChargeService(_wintechClient, credentials);
    }
}

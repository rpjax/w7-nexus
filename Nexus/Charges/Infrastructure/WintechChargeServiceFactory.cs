using Nexus.Charges.Application;
using Nexus.Legacy.Wintech.Application;
using Nexus.Legacy.Wintech.Application.Models;

namespace Nexus.Charges.Infrastructure;

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

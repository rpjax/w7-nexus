using Nexus.AppHost;
using Nexus.Charges.Infrastructure;
using Nexus.Gateways.Application;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Legacy.Charges.Infrastructure;

namespace Nexus.Gateways.Infrastructure;

public sealed class FrendzChargeServiceFactory : IFrendzChargeServiceFactory
{
    private IFrendzClient _frendzClient { get; }
    private IAppHostProvider _appHostProvider { get; }

    public FrendzChargeServiceFactory(IFrendzClient frendzClient, IAppHostProvider appHostProvider)
    {
        _frendzClient = frendzClient;
        _appHostProvider = appHostProvider;
    }

    public IChargeService Create(FrendzApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new FrendzChargeService(_frendzClient, _appHostProvider, credentials);
    }
}

using Nexus.AppHost;
using Nexus.Legacy.Charges.Application;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.Frendz.Application.Models;

namespace Nexus.Legacy.Charges.Infrastructure;

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

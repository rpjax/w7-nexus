using Nexus.Charges.Application;
using Nexus.Frendz.Application;
using Nexus.Frendz.Application.Models;

namespace Nexus.Charges.Infrastructure;

public sealed class FrendzChargeServiceFactory : IFrendzChargeServiceFactory
{
    private IFrendzClient _frendzClient { get; }

    public FrendzChargeServiceFactory(IFrendzClient frendzClient)
    {
        _frendzClient = frendzClient;
    }

    public IChargeService Create(FrendzApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new FrendzChargeService(_frendzClient, credentials);
    }
}

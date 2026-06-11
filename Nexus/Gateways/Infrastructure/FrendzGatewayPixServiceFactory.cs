using Nexus.AppHost;
using Nexus.Gateways.Application;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Infrastructure;

public sealed class FrendzGatewayPixServiceFactory : IFrendzGatewayPixServiceFactory
{
    private IFrendzClient _frendzClient { get; }
    private IAppHostProvider _appHostProvider { get; }

    public FrendzGatewayPixServiceFactory(IFrendzClient frendzClient, IAppHostProvider appHostProvider)
    {
        _frendzClient = frendzClient;
        _appHostProvider = appHostProvider;
    }

    public IGatewayPixService Create(FrendzApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new FrendzGatewayPixService(_frendzClient, _appHostProvider, credentials);
    }
}

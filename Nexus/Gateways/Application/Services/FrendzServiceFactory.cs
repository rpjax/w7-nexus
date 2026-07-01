using Nexus.AppHost;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.AppHost.Contracts;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class FrendzServiceFactory : IFrendzServiceFactory
{
    private IFrendzClient _frendzClient { get; }
    private IAppHostProvider _appHostProvider { get; }

    public FrendzServiceFactory(IFrendzClient frendzClient, IAppHostProvider appHostProvider)
    {
        _frendzClient = frendzClient;
        _appHostProvider = appHostProvider;
    }

    public Task<IGatewayService> CreateAsync(FrendzApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return Task.FromResult<IGatewayService>(new FrendzService(_frendzClient, _appHostProvider, credentials));
    }
}

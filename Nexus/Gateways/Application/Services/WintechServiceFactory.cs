using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class WintechServiceFactory : IWintechServiceFactory
{
    private IWintechClient _wintechClient { get; }

    public WintechServiceFactory(IWintechClient wintechClient)
    {
        _wintechClient = wintechClient;
    }

    public Task<IGatewayService> CreateAsync(WintechApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return Task.FromResult<IGatewayService>(new WintechService(_wintechClient, credentials));
    }
}

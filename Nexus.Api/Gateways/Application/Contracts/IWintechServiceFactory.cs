using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IWintechServiceFactory
{
    Task<IGatewayService> CreateAsync(WintechApiCredentials credentials);
}

using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface IFrendzServiceFactory
{
    Task<IGatewayService> CreateAsync(FrendzApiCredentials credentials);
}

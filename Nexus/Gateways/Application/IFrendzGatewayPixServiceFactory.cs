using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Application;

public interface IFrendzGatewayPixServiceFactory
{
    IGatewayPixService Create(FrendzApiCredentials credentials);
}

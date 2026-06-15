using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Contracts;

public interface IFrendzGatewayPixServiceFactory
{
    IGatewayPixService Create(FrendzApiCredentials credentials);
}

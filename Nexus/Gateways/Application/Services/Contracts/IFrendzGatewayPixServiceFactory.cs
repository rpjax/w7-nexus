using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Application.Services.Contracts;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IFrendzGatewayPixServiceFactory
{
    IGatewayPixService Create(FrendzApiCredentials credentials);
}

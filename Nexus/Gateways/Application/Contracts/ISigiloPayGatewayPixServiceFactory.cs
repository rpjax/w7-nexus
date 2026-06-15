using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Application.Contracts;

namespace Nexus.Gateways.Application.Contracts;

public interface ISigiloPayGatewayPixServiceFactory
{
    IGatewayPixService Create(SigiloPayApiCredentials credentials);
}

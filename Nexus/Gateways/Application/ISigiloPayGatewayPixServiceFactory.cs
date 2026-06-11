using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Application;

public interface ISigiloPayGatewayPixServiceFactory
{
    IGatewayPixService Create(SigiloPayApiCredentials credentials);
}

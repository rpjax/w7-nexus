using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Application.Services.Contracts;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface ISigiloPayGatewayPixServiceFactory
{
    IGatewayPixService Create(SigiloPayApiCredentials credentials);
}

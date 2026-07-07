using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Application.Contracts;

public interface ISigiloPayServiceFactory
{
    Task<IGatewayService> CreateAsync(SigiloPayApiCredentials credentials);
}

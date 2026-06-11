using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Application;

public interface ISigiloPayChargeServiceFactory
{
    IChargeService Create(SigiloPayApiCredentials credentials);
}

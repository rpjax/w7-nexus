using Nexus.Legacy.SigiloPay.Application.Models;

namespace Nexus.Charges.Application;

public interface ISigiloPayChargeServiceFactory
{
    IChargeService Create(SigiloPayApiCredentials credentials);
}

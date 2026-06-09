using Nexus.Legacy.SigiloPay.Application.Models;

namespace Nexus.Legacy.Charges.Application;

public interface ISigiloPayChargeServiceFactory
{
    IChargeService Create(SigiloPayApiCredentials credentials);
}

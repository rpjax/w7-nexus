using Nexus.Legacy.Frendz.Application.Models;

namespace Nexus.Legacy.Charges.Application;

public interface IFrendzChargeServiceFactory
{
    IChargeService Create(FrendzApiCredentials credentials);
}

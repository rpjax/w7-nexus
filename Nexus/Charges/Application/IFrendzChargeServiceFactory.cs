using Nexus.Legacy.Frendz.Application.Models;

namespace Nexus.Charges.Application;

public interface IFrendzChargeServiceFactory
{
    IChargeService Create(FrendzApiCredentials credentials);
}

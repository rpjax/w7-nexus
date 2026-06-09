using Nexus.Legacy.Wintech.Application.Models;

namespace Nexus.Legacy.Charges.Application;

public interface IWintechChargeServiceFactory
{
    IChargeService Create(WintechApiCredentials credentials);
}

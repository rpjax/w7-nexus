using Nexus.Wintech.Application.Models;

namespace Nexus.Charges.Application;

public interface IWintechChargeServiceFactory
{
    IChargeService Create(WintechApiCredentials credentials);
}

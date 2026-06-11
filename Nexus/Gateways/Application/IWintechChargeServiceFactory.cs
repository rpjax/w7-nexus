using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Application;

public interface IWintechChargeServiceFactory
{
    IChargeService Create(WintechApiCredentials credentials);
}

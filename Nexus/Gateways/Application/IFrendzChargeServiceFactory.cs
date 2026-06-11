using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Application;

public interface IFrendzChargeServiceFactory
{
    IChargeService Create(FrendzApiCredentials credentials);
}

using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application;

public interface IChargeService
{
    Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request);
}

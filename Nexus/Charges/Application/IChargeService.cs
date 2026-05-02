using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application;

public interface IChargeService
{
    Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request);
}

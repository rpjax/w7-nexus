using Nexus.Legacy.Charges.Application.Models;

namespace Nexus.Legacy.Charges.Application;

public interface IChargeService
{
    Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request);
}

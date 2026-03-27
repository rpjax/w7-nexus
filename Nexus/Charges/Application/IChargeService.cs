using Nexus.Charges.Application.Models;
using Nexus.Frendz.Application;

namespace Nexus.Charges.Application;

public interface IChargeService
{
    Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request);
}

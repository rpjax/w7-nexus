using Aidan.Core.Patterns;
using Nexus.Legacy.Charges.Application.Models;

namespace Nexus.Legacy.Charges.Application;

public interface IChargeOrchestrator
{
    Task<IResult<PixCharge>> CreatePixChargeAsync(CreatePixChargeRequest request);
}

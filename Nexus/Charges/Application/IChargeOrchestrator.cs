using Aidan.Core.Patterns;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application;

public interface IChargeOrchestrator
{
    Task<IResult<PixCharge>> CreatePixChargeAsync(CreatePixChargeRequest request);
}

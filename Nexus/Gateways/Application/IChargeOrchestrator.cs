using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;

namespace Nexus.Gateways.Application;

public interface IChargeOrchestrator
{
    Task<IResult<PixCharge>> CreatePixChargeAsync(CreatePixChargeRequest request);
}

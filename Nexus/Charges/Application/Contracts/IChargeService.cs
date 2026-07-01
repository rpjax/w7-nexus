using Aidan.Core.Patterns;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application.Contracts;

public interface IChargeService
{
    Task<IResult<CreatePixChargeResponse>> CreatePixChargeAsync(CreatePixChargeRequest request);
}

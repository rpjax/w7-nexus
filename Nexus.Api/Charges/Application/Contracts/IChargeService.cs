using Aidan.Core.Patterns;
using Nexus.Charges.Application.Requests;
using Nexus.Charges.Application.Responses;

namespace Nexus.Charges.Application.Contracts;

public interface IChargeService
{
    Task<IResult<CreatePixChargeResponse>> CreatePixChargeAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default);
}

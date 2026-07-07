using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Charges.Application.Models;

namespace Nexus.Charges.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<CreatePixChargeResponse>> CreatePixChargeAsync(
        RequesterIdentity identity,
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default);
}

using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Withdrawals.Application.Contracts;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministratorWithdrawalCommandService
{
    Task<IOperationResult<Withdrawals.Aggregates.Withdrawal>> CreateWithdrawalAsync(
        RequesterIdentity identity,
        CreateWithdrawalRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Withdrawals.Aggregates.Withdrawal>> GetWithdrawalAsync(
        RequesterIdentity identity,
        string withdrawalId,
        CancellationToken cancellationToken = default);
}

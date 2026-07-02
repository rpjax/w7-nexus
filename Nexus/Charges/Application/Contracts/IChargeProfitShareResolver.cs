using Aidan.Core.Patterns;
using Nexus.Payments.Aggregates;

namespace Nexus.Charges.Application.Contracts;

public interface IChargeProfitShareResolver
{
    Task<IResult<IReadOnlyList<PaymentSplit>>> ResolveSplitsAsync(
        string operationId,
        string? operatorId,
        decimal amount,
        CancellationToken cancellationToken = default);
}

using Nexus.Payments.Aggregates;

namespace Nexus.Charges.Application.Contracts;

public interface IChargeSplitCalculationService
{
    Task<IReadOnlyList<PaymentSplit>> ApplyStrawManFeeAsync(
        decimal amount,
        IReadOnlyList<PaymentSplit> profitShareSplits,
        string strawManId,
        CancellationToken cancellationToken = default);
}

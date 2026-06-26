using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentSplitCalculationService
{
    Task<IReadOnlyList<PaymentSplit>> ApplyStrawManFeeAsync(
        decimal amount,
        IReadOnlyList<PaymentSplit> profitShareSplits,
        string strawManId,
        CancellationToken cancellationToken = default);
}

using Aidan.Core.Patterns;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Contracts;

public interface IBalanceSplitCalculationService
{
    Task<IResult<BalanceSplitCalculationResult>> CalculateForCreditAsync(
        string destinationStrawManId,
        decimal amount,
        IReadOnlyList<TransferBalanceSplit> originalSplits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CancellationToken cancellationToken = default);
}

public sealed class BalanceSplitCalculationResult
{
    public IReadOnlyList<TransferBalanceSplit> Splits { get; init; } = Array.Empty<TransferBalanceSplit>();
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; init; } = Array.Empty<string>();
}

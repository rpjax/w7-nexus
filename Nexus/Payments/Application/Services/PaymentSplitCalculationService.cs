using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.Payments.Application.Services;

public sealed class PaymentSplitCalculationService : IPaymentSplitCalculationService
{
    private readonly IStrawManSettingsQueryService _strawManSettings;

    public PaymentSplitCalculationService(IStrawManSettingsQueryService strawManSettings)
    {
        _strawManSettings = strawManSettings;
    }

    public async Task<IReadOnlyList<PaymentSplit>> ApplyStrawManFeeAsync(
        decimal amount,
        IReadOnlyList<PaymentSplit> profitShareSplits,
        string strawManId,
        CancellationToken cancellationToken = default)
    {
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(strawManId) || profitShareSplits.Count == 0)
            return profitShareSplits;

        if (profitShareSplits.Any(s =>
                s.SplitKind == PaymentSplitKind.StrawManFee
                && string.Equals(s.AccountId, strawManId, StringComparison.Ordinal)))
        {
            return profitShareSplits;
        }

        var feePct = await _strawManSettings.GetMovementFeePercentageAsync(strawManId, cancellationToken);
        if (feePct <= 0)
            return profitShareSplits;

        var dilutionFactor = (100m - feePct) / 100m;
        var dilutedSplits = new List<PaymentSplit>(profitShareSplits.Count + 1);

        foreach (var split in profitShareSplits)
        {
            if (split.SplitKind == PaymentSplitKind.StrawManFee)
                continue;

            var newPct = Round(split.Percentage * dilutionFactor);
            var newAmount = Round(amount * newPct / 100m);
            dilutedSplits.Add(new PaymentSplit(
                split.AccountId,
                newPct,
                newAmount,
                PaymentSplitKind.ProfitShare));
        }

        var feeAmount = Round(amount * feePct / 100m);
        dilutedSplits.Add(new PaymentSplit(
            strawManId,
            feePct,
            feeAmount,
            PaymentSplitKind.StrawManFee));

        return dilutedSplits;
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

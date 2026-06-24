using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Errors;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.AccountNodes.Application.Services;

public sealed class BalanceSplitCalculationService : IBalanceSplitCalculationService
{
    private readonly IStrawManSettingsQueryService _strawManSettings;

    public BalanceSplitCalculationService(IStrawManSettingsQueryService strawManSettings)
    {
        _strawManSettings = strawManSettings;
    }

    public async Task<IResult<BalanceSplitCalculationResult>> CalculateForCreditAsync(
        string destinationStrawManId,
        decimal amount,
        IReadOnlyList<BalanceSplitSnapshot> originalSplits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CancellationToken cancellationToken = default)
    {
        destinationStrawManId = destinationStrawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(destinationStrawManId))
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.OriginStrawManIdInvalid)
                .WithMessage("O ID do laranja de destino é obrigatório.")
                .Build());

        if (amount <= 0)
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor para cálculo de split deve ser maior que zero.")
                .Build());

        if (originalSplits.Count == 0)
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceSplitSnapshotRequired)
                .WithMessage("É necessário informar ao menos um split de origem.")
                .Build());

        var feeIds = (appliedStrawManFeeIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var splits = originalSplits.ToList();

        if (!feeIds.Contains(destinationStrawManId, StringComparer.Ordinal))
        {
            var feePct = await _strawManSettings.GetMovementFeePercentageAsync(
                destinationStrawManId,
                cancellationToken);

            if (feePct > 0)
            {
                feeIds.Add(destinationStrawManId);
                var dilutionFactor = (100m - feePct) / 100m;
                var dilutedSplits = new List<BalanceSplitSnapshot>(splits.Count);

                foreach (var split in splits)
                {
                    var newPct = Round(split.Percentage * dilutionFactor);
                    var newAmount = Round(amount * newPct / 100m);
                    var diluted = BalanceSplitSnapshot.Create(
                        split.AccountId,
                        newPct,
                        newAmount,
                        split.SplitKind);

                    if (diluted.IsFailure)
                        return Result<BalanceSplitCalculationResult>.Failure(diluted.Errors);

                    dilutedSplits.Add(diluted.Value!);
                }

                var feeAmount = Round(amount * feePct / 100m);
                var feeSplit = BalanceSplitSnapshot.Create(
                    destinationStrawManId,
                    feePct,
                    feeAmount,
                    SplitKind.StrawManMovementFee);

                if (feeSplit.IsFailure)
                    return Result<BalanceSplitCalculationResult>.Failure(feeSplit.Errors);

                dilutedSplits.Add(feeSplit.Value!);
                splits = dilutedSplits;
            }
        }

        return Result<BalanceSplitCalculationResult>.Success(new BalanceSplitCalculationResult
        {
            SplitSnapshot = splits,
            AppliedStrawManFeeIds = feeIds,
        });
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

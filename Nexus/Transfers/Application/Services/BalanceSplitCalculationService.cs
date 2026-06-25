using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.StrawMen.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

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
        IReadOnlyList<TransferBalanceSplit> originalSplits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CancellationToken cancellationToken = default)
    {
        destinationStrawManId = destinationStrawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(destinationStrawManId))
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OriginStrawManIdInvalid)
                .WithMessage("O ID do laranja de destino é obrigatório.")
                .Build());

        if (amount <= 0)
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SourceAmountInvalid)
                .WithMessage("O valor para cálculo de split deve ser maior que zero.")
                .Build());

        if (originalSplits.Count == 0)
            return Result<BalanceSplitCalculationResult>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BalanceSplitsRequired)
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
                var dilutedSplits = new List<TransferBalanceSplit>(splits.Count);

                foreach (var split in splits)
                {
                    var newPct = Round(split.Percentage * dilutionFactor);
                    var newAmount = Round(amount * newPct / 100m);
                    var diluted = TransferBalanceSplit.Create(
                        split.AccountId,
                        newPct,
                        newAmount,
                        split.SplitKind);

                    if (diluted.IsFailure)
                        return Result<BalanceSplitCalculationResult>.Failure(diluted.Errors);

                    dilutedSplits.Add(diluted.Value!);
                }

                var feeAmount = Round(amount * feePct / 100m);
                var feeSplit = TransferBalanceSplit.Create(
                    destinationStrawManId,
                    feePct,
                    feeAmount,
                    TransferSplitKind.StrawManMovementFee);

                if (feeSplit.IsFailure)
                    return Result<BalanceSplitCalculationResult>.Failure(feeSplit.Errors);

                dilutedSplits.Add(feeSplit.Value!);
                splits = dilutedSplits;
            }
        }

        return Result<BalanceSplitCalculationResult>.Success(new BalanceSplitCalculationResult
        {
            Splits = splits,
            AppliedStrawManFeeIds = feeIds,
        });
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

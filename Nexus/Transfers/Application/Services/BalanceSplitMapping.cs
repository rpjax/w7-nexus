using Nexus.BankAccounts.Aggregates;
using Nexus.CryptoWallets.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Services;

internal static class BalanceSplitMapping
{
    public static IReadOnlyList<TransferBalanceSplit> FromBankSplits(IReadOnlyList<BankBalanceSplit> splits) =>
        splits.Select(s => new TransferBalanceSplit(
            s.AccountId,
            s.Percentage,
            s.Amount,
            s.SplitKind == BankSplitKind.ProfitShare
                ? TransferSplitKind.ProfitShare
                : TransferSplitKind.StrawManMovementFee)).ToList();

    public static IReadOnlyList<TransferBalanceSplit> FromCryptoSplits(IReadOnlyList<CryptoBalanceSplit> splits) =>
        splits.Select(s => new TransferBalanceSplit(
            s.AccountId,
            s.Percentage,
            s.Amount,
            s.SplitKind == CryptoSplitKind.ProfitShare
                ? TransferSplitKind.ProfitShare
                : TransferSplitKind.StrawManMovementFee)).ToList();

    public static IReadOnlyList<BankBalanceSplit> ToBankSplits(IReadOnlyList<TransferBalanceSplit> splits)
    {
        var result = new List<BankBalanceSplit>(splits.Count);
        foreach (var split in splits)
        {
            var mapped = BankBalanceSplit.Create(
                split.AccountId,
                split.Percentage,
                split.Amount,
                split.SplitKind == TransferSplitKind.ProfitShare
                    ? BankSplitKind.ProfitShare
                    : BankSplitKind.StrawManMovementFee);

            if (mapped.IsFailure)
                throw new InvalidOperationException(string.Join("; ", mapped.Errors.Select(e => e.Message)));

            result.Add(mapped.Value!);
        }

        return result;
    }

    public static IReadOnlyList<CryptoBalanceSplit> ToCryptoSplits(IReadOnlyList<TransferBalanceSplit> splits)
    {
        var result = new List<CryptoBalanceSplit>(splits.Count);
        foreach (var split in splits)
        {
            var mapped = CryptoBalanceSplit.Create(
                split.AccountId,
                split.Percentage,
                split.Amount,
                split.SplitKind == TransferSplitKind.ProfitShare
                    ? CryptoSplitKind.ProfitShare
                    : CryptoSplitKind.StrawManMovementFee);

            if (mapped.IsFailure)
                throw new InvalidOperationException(string.Join("; ", mapped.Errors.Select(e => e.Message)));

            result.Add(mapped.Value!);
        }

        return result;
    }
}

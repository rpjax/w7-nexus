using Nexus.Database.Models;

namespace Nexus.Operations.Aggregates;

public static class ProfitSharePercentageRules
{
    public const decimal TotalTarget = 100m;
    public const decimal TotalTolerance = 0.05m;
    public const decimal MinCutPercentage = 0.01m;
    public const decimal MaxCutPercentage = 100m;

    public static bool IsTotalValid(decimal total)
        => Math.Abs(total - TotalTarget) <= TotalTolerance;

    public static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static List<ProfitSplitRecord> NormalizeCuts(IReadOnlyList<ProfitSplitRecord> cuts)
    {
        var normalized = cuts
            .Select(cut => new ProfitSplitRecord
            {
                AccountId = cut.AccountId,
                Percentage = Round(cut.Percentage),
            })
            .ToList();

        if (normalized.Count == 0)
            return normalized;

        var total = normalized.Sum(cut => cut.Percentage);
        var diff = Round(TotalTarget - total);
        if (diff == 0m || Math.Abs(diff) > TotalTolerance)
            return normalized;

        var lastIndex = normalized.Count - 1;
        var last = normalized[lastIndex];
        normalized[lastIndex] = new ProfitSplitRecord
        {
            AccountId = last.AccountId,
            Percentage = Round(last.Percentage + diff),
        };

        return normalized;
    }
}

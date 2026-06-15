using Nexus.Database.Models;

namespace Nexus.Operations.Aggregates;

public sealed class ProfitShareRule
{
    public string OperatorId { get; }
    public IReadOnlyDictionary<string, ProfitSplit> ProfitSplits { get; }

    internal ProfitShareRule(
        string operatorId, 
        IReadOnlyDictionary<string, ProfitSplit> profitSplits)
    {
        OperatorId = operatorId;
        ProfitSplits = profitSplits;
    }

    internal static ProfitShareRule FromRecord(OperatorProfitShareRuleRecord record)
    {
        var cuts = new Dictionary<string, ProfitSplit>(StringComparer.Ordinal);

        foreach (var cut in record.Cuts ?? new List<ProfitSplitRecord>())
        {
            if (string.IsNullOrWhiteSpace(cut.AccountId))
                continue;

            var accountId = cut.AccountId.Trim();

            if (cuts.ContainsKey(accountId))
                continue;

            cuts[accountId] = new ProfitSplit(accountId, cut.Percentage);
        }

        return new ProfitShareRule(record.OperatorId.Trim(), cuts);
    }
}

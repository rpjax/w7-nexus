namespace Nexus.Operations.Aggregates;

public sealed class ProfitSplit
{
    public string AccountId { get; }
    public decimal Percentage { get; }

    public ProfitSplit(string accountId, decimal percentage)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
    }
}

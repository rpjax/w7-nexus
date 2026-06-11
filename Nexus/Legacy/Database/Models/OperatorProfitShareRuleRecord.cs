namespace Nexus.Legacy.Database.Models;

public sealed class OperatorProfitShareRuleRecord
{
    public string OperatorId { get; set; } = string.Empty;
    public List<ProfitSplitRecord> Cuts { get; set; } = new();
}

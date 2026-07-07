namespace Nexus.Database.Models;

public sealed class ProfitSplitRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

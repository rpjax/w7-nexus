namespace Nexus.Database.Models;

public sealed class PaymentSplitRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
}

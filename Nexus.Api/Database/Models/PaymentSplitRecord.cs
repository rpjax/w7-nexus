namespace Nexus.Database.Models;

using Nexus.Payments.Aggregates;

public sealed class PaymentSplitRecord
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public PaymentSplitKind SplitKind { get; set; }
}

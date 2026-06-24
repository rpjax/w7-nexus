namespace Nexus.Payments.Application.Models;

public sealed class PaymentSplitDetails
{
    public string AccountId { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public decimal Amount { get; init; }
}

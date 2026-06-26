namespace Nexus.Payments.Application.Models;

public sealed class PaymentSplitDetails
{
    public string AccountId { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Role { get; init; }
    public decimal Percentage { get; init; }
    public decimal Amount { get; init; }
}

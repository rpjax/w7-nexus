namespace Nexus.Payments.Application.Models;

public sealed class SearchPaymentsResponse
{
    public int Offset { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<PaymentDetails> Items { get; init; } = Array.Empty<PaymentDetails>();
}

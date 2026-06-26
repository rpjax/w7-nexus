using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Models;

public class SearchPaymentsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentSettlementStatus? SettlementStatus { get; set; }
    public PaymentDistributionStatus? DistributionStatus { get; set; }
    public string? OperationId { get; set; }
    public string? StrawManId { get; set; }
}

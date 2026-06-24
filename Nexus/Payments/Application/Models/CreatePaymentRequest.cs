using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application.Models;

public class CreatePaymentRequest
{
    /// <summary>When set, used as the aggregate payment id (e.g. before calling the gateway).</summary>
    public string? ExplicitPaymentId { get; set; }

    public string? OperationId { get; set; }
    public string? OperatorId { get; set; }
    public string? StrawManId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string? GatewayPaymentId { get; set; }
}

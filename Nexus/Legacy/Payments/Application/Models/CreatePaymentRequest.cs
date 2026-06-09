using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Legacy.Payments.Application.Models;

public class CreatePaymentRequest
{
    /// <summary>When set, used as the aggregate payment id (e.g. before calling the gateway).</summary>
    public string? ExplicitPaymentId { get; set; }

    public string? OperationId { get; set; }
    public string? OperatorAccountId { get; set; }
    public string? StrawManAccountId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string? GatewayPaymentId { get; set; }
}
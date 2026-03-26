using Nexus.Payments.Aggregates;

namespace Nexus.Payments.Application;

public class CreatePixPaymentRequest
{
    public string? OperationId { get; set; }
    public string? OperatorAccountId { get; set; }
    public string? StrawManAccountId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string? GatewayPaymentId { get; set; }
}
namespace Nexus.PaymentGateways.Application.Models;

public sealed class CreateGatewayPixPaymentRequest
{
    public string OperationId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? OperatorAccountId { get; init; }
    public string? StrawManAccountId { get; init; }

    public string OfferHash { get; init; } = string.Empty;
    public string ProductHash { get; init; } = string.Empty;
    public string ProductTitle { get; init; } = string.Empty;
    public string? PostbackUrl { get; init; }
    public int ExpireInDays { get; init; } = 1;

    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhoneNumber { get; init; } = string.Empty;
    public string CustomerDocument { get; init; } = string.Empty;
}

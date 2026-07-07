namespace Nexus.Payments.Application.Models;

public sealed class KillPaymentRequest
{
    public string Reason { get; set; } = string.Empty;
}

namespace Nexus.Olx.Application.Requests.Victim;

public class CreatePixPaymentRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string? AdId { get; set; }
    public decimal Value { get; set; }
}
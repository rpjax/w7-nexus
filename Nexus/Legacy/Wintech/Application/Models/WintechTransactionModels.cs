namespace Nexus.Legacy.Wintech.Application.Models;

/// <summary>Cliente exigido pelo POST /gateway/pix/receive (valores em reais no pedido).</summary>
public sealed class WintechCustomerInfo
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
}

public sealed class WintechPixPaymentRequest
{
    public string Identifier { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public WintechCustomerInfo Client { get; init; } = null!;
}

public sealed class WintechPixPaymentResult
{
    public string TransactionId { get; init; } = string.Empty;
    public string PixCode { get; init; } = string.Empty;
}

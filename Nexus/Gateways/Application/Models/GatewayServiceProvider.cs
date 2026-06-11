using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Gateways.Application.Models;

/// <summary>
/// Associa um serviço de cobrança ao gateway e ao straw man da credencial usada.
/// O orchestrator só faz bind no pagamento depois que a cobrança é criada com sucesso.
/// </summary>
public sealed class GatewayServiceProvider
{
    public PaymentGateway Gateway { get; }
    public string? StrawManId { get; }
    public IGatewayPixService Service { get; }

    public GatewayServiceProvider(PaymentGateway gateway, string? strawManId, IGatewayPixService service)
    {
        Gateway = gateway;
        StrawManId = strawManId;
        Service = service;
    }
}

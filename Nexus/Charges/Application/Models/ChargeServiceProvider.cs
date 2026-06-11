using Nexus.Charges.Application;
using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Charges.Application.Models;

/// <summary>
/// Associa um serviço de cobrança ao gateway e ao straw man da credencial usada.
/// O orchestrator só faz bind no pagamento depois que a cobrança é criada com sucesso.
/// </summary>
public sealed class ChargeServiceProvider
{
    public PaymentGateway Gateway { get; }
    public string? StrawManId { get; }
    public IChargeService Service { get; }

    public ChargeServiceProvider(PaymentGateway gateway, string? strawManId, IChargeService service)
    {
        Gateway = gateway;
        StrawManId = strawManId;
        Service = service;
    }
}

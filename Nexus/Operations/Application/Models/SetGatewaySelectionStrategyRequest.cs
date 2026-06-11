using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Models;

public class SetGatewaySelectionStrategyRequest
{
    public string? TeamId { get; set; }
    public GatewaySelectionStrategy Strategy { get; set; }
}

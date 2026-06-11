using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Models;

public class SetGatewaySelectionStrategyRequest
{
    public string TeamId { get; set; } = string.Empty;
    public GatewaySelectionStrategy Strategy { get; set; }
}

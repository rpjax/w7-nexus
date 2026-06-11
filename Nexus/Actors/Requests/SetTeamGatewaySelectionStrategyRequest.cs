using Nexus.Operations.Aggregates;

namespace Nexus.Actors.Requests;

public class SetTeamGatewaySelectionStrategyRequest
{
    public string? TeamId { get; set; }
    public GatewaySelectionStrategy Strategy { get; set; }
}

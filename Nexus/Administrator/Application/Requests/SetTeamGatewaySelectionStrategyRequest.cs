using Nexus.Operations.Aggregates;

namespace Nexus.Administrator.Application.Requests;

public class SetTeamGatewaySelectionStrategyRequest
{
    public string TeamId { get; set; } = string.Empty;
    public GatewaySelectionStrategy Strategy { get; set; }
}

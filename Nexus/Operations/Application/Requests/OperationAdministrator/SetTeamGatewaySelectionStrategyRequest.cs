using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Requests.OperationAdministrator;

public class SetTeamGatewaySelectionStrategyRequest
{
    public string TeamId { get; set; } = string.Empty;
    public GatewaySelectionStrategy Strategy { get; set; }
}

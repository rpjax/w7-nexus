using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Requests.OperationAdministrator;

public class SetOperationGatewaySelectionStrategyRequest
{
    public string OperationId { get; set; } = string.Empty;
    public GatewaySelectionStrategy Strategy { get; set; }
}

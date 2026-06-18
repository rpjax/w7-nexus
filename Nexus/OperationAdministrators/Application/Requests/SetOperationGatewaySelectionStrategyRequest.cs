using Nexus.Operations.Aggregates;

namespace Nexus.OperationAdministrators.Application.Requests;

public class SetOperationGatewaySelectionStrategyRequest
{
    public string OperationId { get; set; } = string.Empty;
    public GatewaySelectionStrategy Strategy { get; set; }
}

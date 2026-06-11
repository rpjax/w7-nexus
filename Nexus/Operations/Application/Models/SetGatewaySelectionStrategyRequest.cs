using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Models;

public class SetGatewaySelectionStrategyRequest
{
    public string OperationId { get; set; } = default!;
    public OperationGatewaySelectionStrategy Strategy { get; set; }
}

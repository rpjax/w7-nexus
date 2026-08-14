using Refactor.Nexus.Api.Charging.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Services;

namespace Refactor.Nexus.Api.Charging.Infrastructure.Operations;

public sealed class OperationChargingDirectoryAdapter : IOperationChargingDirectory
{
    private readonly IOperationReadRepository _operations;
    private readonly IOperationActivityPolicy _activity;

    public OperationChargingDirectoryAdapter(IOperationReadRepository operations, IOperationActivityPolicy activity)
    {
        _operations = operations;
        _activity = activity;
    }

    public async Task<OperationChargingSnapshot?> GetAsync(
        Guid operationId,
        Guid operatorMemberId,
        CancellationToken cancellationToken = default)
    {
        var operation = await _operations.GetByIdAsync(new OperationId(operationId), cancellationToken);
        if (operation is null)
            return null;

        return new OperationChargingSnapshot(
            operation.Id.Value,
            _activity.AllowsNewCharging(operation.Status),
            operation.ManagementCutPercent,
            operation.AssignedOperatorIds.Contains(operatorMemberId));
    }
}

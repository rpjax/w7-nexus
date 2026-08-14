using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;

namespace Refactor.Nexus.Api.Mandates.Infrastructure.Operations;

public sealed class OperationDirectoryAdapter : IOperationDirectory, IOperationAssignmentProbe
{
    private readonly IOperationReadRepository _operations;

    public OperationDirectoryAdapter(IOperationReadRepository operations)
    {
        _operations = operations;
    }

    public Task<bool> ExistsAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        _operations.ExistsAsync(new OperationId(operationId), cancellationToken);

    public Task<bool> IsMemberAssignedAsync(Guid operationId, Guid memberId, CancellationToken cancellationToken = default) =>
        _operations.IsMemberAssignedAsync(new OperationId(operationId), memberId, cancellationToken);

    public Task<bool> IsMemberAssignedToAnyAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _operations.IsMemberAssignedToAnyAsync(memberId, cancellationToken);
}

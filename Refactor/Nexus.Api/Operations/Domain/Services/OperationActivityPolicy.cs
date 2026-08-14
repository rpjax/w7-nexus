using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;

namespace Refactor.Nexus.Api.Operations.Domain.Services;

/// <summary>
/// Hook for etapa 04+ Cobrança: only Active ops accept new charging.
/// </summary>
public interface IOperationActivityPolicy
{
    bool AllowsNewCharging(OperationStatus status);
    bool AllowsScriptResolve(OperationStatus status);
    bool AllowsStoreWrite(OperationStatus status);
}

public sealed class OperationActivityPolicy : IOperationActivityPolicy
{
    public bool AllowsNewCharging(OperationStatus status) => status == OperationStatus.Active;
    public bool AllowsScriptResolve(OperationStatus status) => status == OperationStatus.Active;
    public bool AllowsStoreWrite(OperationStatus status) =>
        status is OperationStatus.Draft or OperationStatus.Active;
}

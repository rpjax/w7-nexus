using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Application.UseCases.Shared;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Operations.Domain.Services;

namespace Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Queries;

public sealed record OperationView(
    string OperationId,
    string OperationKey,
    string Name,
    string Status,
    decimal? ManagementCutPercent,
    IReadOnlyList<string> AssignedOperatorIds,
    bool AllowsNewCharging,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record ListOperationsResult(IReadOnlyList<OperationView> Items);
public interface IListOperationsUseCase
{
    Task<IOperationResult<ListOperationsResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class ListOperationsHandler : IListOperationsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;

    public ListOperationsHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations)
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
    }

    public async Task<IOperationResult<ListOperationsResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var access = await OperationAccessGuards.AuthorizeManageAsync<ListOperationsResult>(
            _requestContext, _gate, operationId: null, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var items = await _operations.ListAsync(cancellationToken);
        var requesterId = Guid.Parse(access.Requester!.AccountId);
        var isAdmin = await _gate.IsAdministratorAsync(requesterId, cancellationToken);

        var views = new List<OperationView>();
        foreach (var op in items)
        {
            if (!isAdmin && !await _gate.CanManageOperationAsync(requesterId, op.Id, cancellationToken))
                continue;
            views.Add(ToView(op));
        }

        return OperationResult<ListOperationsResult>.Success(new ListOperationsResult(views));
    }

    internal static OperationView ToView(Domain.Aggregates.Operation.Operation op) =>
        new(
            op.Id.ToString(),
            op.Key.Value,
            op.Name,
            op.Status.ToString(),
            op.ManagementCutPercent,
            op.AssignedOperatorIds.Select(id => id.ToString()).ToList(),
            op.AllowsNewCharging,
            op.CreatedAt,
            op.LastUpdatedAt);
}

public sealed record GetOperationQuery(string OperationId);
public interface IGetOperationUseCase
{
    Task<IOperationResult<OperationView>> HandleAsync(GetOperationQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetOperationHandler : IGetOperationUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;

    public GetOperationHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations)
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
    }

    public async Task<IOperationResult<OperationView>> HandleAsync(
        GetOperationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!OperationId.TryParse(query.OperationId, out var operationId))
            return OperationResult<OperationView>.Failure(OperationAccessGuards.NotFound(query.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<OperationView>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _operations.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<OperationView>.Failure(OperationAccessGuards.NotFound(query.OperationId));

        return OperationResult<OperationView>.Success(ListOperationsHandler.ToView(operation));
    }
}

public sealed record ListStoreObjectsQuery(string OperationId, string? ObjectType);
public sealed record StoreObjectView(string ObjectId, string OperationKey, string ObjectType, string PayloadJson, DateTime LastUpdatedAt);
public sealed record ListStoreObjectsResult(IReadOnlyList<StoreObjectView> Items);

public interface IListStoreObjectsUseCase
{
    Task<IOperationResult<ListStoreObjectsResult>> HandleAsync(ListStoreObjectsQuery query, CancellationToken cancellationToken = default);
}

public sealed class ListStoreObjectsHandler : IListStoreObjectsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;
    private readonly IStoreObjectRepository _store;

    public ListStoreObjectsHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations,
        IStoreObjectRepository store)
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
        _store = store;
    }

    public async Task<IOperationResult<ListStoreObjectsResult>> HandleAsync(
        ListStoreObjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!OperationId.TryParse(query.OperationId, out var operationId))
            return OperationResult<ListStoreObjectsResult>.Failure(OperationAccessGuards.NotFound(query.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<ListStoreObjectsResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _operations.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<ListStoreObjectsResult>.Failure(OperationAccessGuards.NotFound(query.OperationId));

        var items = await _store.ListByKeyAsync(operation.Key, query.ObjectType, cancellationToken);
        return OperationResult<ListStoreObjectsResult>.Success(new ListStoreObjectsResult(
            items.Select(i => new StoreObjectView(
                i.Id.ToString(), i.OperationKey.Value, i.ObjectType, i.PayloadJson, i.LastUpdatedAt)).ToList()));
    }
}

using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Authorization.Application.Models;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;
using StoreObject = Refactor.Nexus.Api.Operations.Domain.Aggregates.Store.StoreObject;

namespace Refactor.Nexus.Api.Tests.Fakes;

internal sealed class AdminRequestContext : IRequestContext
{
    private readonly Guid _accountId;

    public AdminRequestContext(Guid accountId) => _accountId = accountId;

    public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        IResult<RequesterContext> result = Result<RequesterContext>.Success(
            new RequesterContext(_accountId.ToString(), [Roles.Administrator], []));
        return Task.FromResult(result);
    }
}

internal sealed class AllowAllMandateAccessPolicy : IMandateAccessPolicy
{
    public Task<IAuthorizationResult> AuthorizeAdministratorAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IAuthorizationResult>(AuthorizationResult.Authorized());
}

internal sealed class AdminCapabilityGate : IMandateCapabilityGate
{
    private readonly Guid _adminId;

    public AdminCapabilityGate(Guid adminId) => _adminId = adminId;

    public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(accountId == _adminId);

    public Task<bool> CanManageOperationAsync(
        Guid accountId,
        OperationId operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(accountId == _adminId);

    public Task<bool> HasManagementOverOperationAsync(
        Guid accountId,
        OperationId operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

internal sealed class FixedOperatorEligibility : IOperatorEligibility
{
    private readonly HashSet<Guid> _eligible;

    public FixedOperatorEligibility(params Guid[] eligible) => _eligible = [.. eligible];

    public Task<bool> IsEligibleOperatorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_eligible.Contains(accountId));
}

internal sealed class InMemoryOperationRepository : IOperationRepository, IOperationReadRepository
{
    private readonly EventStreamBag _streams = new();

    public Task<OperationAggregate?> GetByIdAsync(OperationId id, CancellationToken cancellationToken = default)
    {
        var loaded = _streams.Load<OperationAggregate>(id.Value);
        return Task.FromResult(loaded is not null && loaded.Id.Value != Guid.Empty ? loaded : null);
    }

    public Task SaveAsync(OperationAggregate operation, CancellationToken cancellationToken = default)
    {
        _streams.Append(operation.Id.Value, operation.UncommittedEvents);
        operation.ClearUncommitted();
        return Task.CompletedTask;
    }

    public Task<OperationAggregate?> GetByKeyAsync(OperationKey key, CancellationToken cancellationToken = default)
    {
        var match = ListAsync(cancellationToken).Result.FirstOrDefault(o => o.Key.Value == key.Value);
        return Task.FromResult(match);
    }

    public async Task<bool> ExistsAsync(OperationId id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken) is not null;

    public async Task<bool> IsMemberAssignedAsync(
        OperationId operationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var operation = await GetByIdAsync(operationId, cancellationToken);
        return operation is not null && operation.IsAssigned(memberId);
    }

    public async Task<bool> IsMemberAssignedToAnyAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var all = await ListAsync(cancellationToken);
        return all.Any(o => o.IsAssigned(memberId));
    }

    public Task<IReadOnlyList<OperationAggregate>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OperationAggregate> items = _streams.StreamIds
            .Select(id => EventFold.Replay<OperationAggregate>(_streams.Get(id)!))
            .Where(o => o.Id.Value != Guid.Empty)
            .ToList();
        return Task.FromResult(items);
    }
}

internal sealed class InMemoryStoreObjectRepository : IStoreObjectRepository
{
    private readonly Dictionary<Guid, StoreObject> _items = [];

    public Task<StoreObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task SaveAsync(StoreObject storeObject, CancellationToken cancellationToken = default)
    {
        _items[storeObject.Id] = storeObject;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _items.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoreObject>> ListByKeyAsync(
        OperationKey key,
        string? objectType,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StoreObject> items = _items.Values
            .Where(i => i.OperationKey.Value == key.Value)
            .Where(i => string.IsNullOrWhiteSpace(objectType) || i.ObjectType == objectType)
            .OrderByDescending(i => i.LastUpdatedAt)
            .ToList();
        return Task.FromResult(items);
    }
}

using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Operations.Domain.Events;

namespace Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;

public sealed class Operation
{
    private readonly HashSet<Guid> _assignedOperatorIds = [];
    private readonly List<object> _uncommitted = [];

    public Operation()
    {
    }

    public OperationId Id { get; private set; }
    public Guid PersistenceId => Id.Value;
    public OperationKey Key { get; private set; }
    public string Name { get; private set; } = "";
    public OperationStatus Status { get; private set; }
    public decimal? ManagementCutPercent { get; private set; }
    public IReadOnlyCollection<Guid> AssignedOperatorIds => _assignedOperatorIds;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public bool IsClosed => Status == OperationStatus.Closed;
    public bool AllowsScriptResolve => Status == OperationStatus.Active;
    public bool AllowsStoreWrite => Status is OperationStatus.Draft or OperationStatus.Active;
    public bool AllowsNewCharging => Status == OperationStatus.Active;

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public void ClearUncommitted() => _uncommitted.Clear();

    public static IResult<Operation> Create(string name, decimal? managementCutPercent = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Operation>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NameEmpty)
                .WithMessage("Nome da Operacao e obrigatorio.")
                .Build());
        }

        var cutCheck = ValidateCut(managementCutPercent);
        if (cutCheck.IsFailure)
            return Result<Operation>.Failure(cutCheck.Errors);

        var operation = new Operation();
        var now = DateTime.UtcNow;
        operation.ApplyChange(new OperationOpened(
            OperationId.New().Value,
            OperationKey.Mint().Value,
            name.Trim(),
            managementCutPercent,
            now,
            null));
        return Result<Operation>.Success(operation);
    }

    public static Operation Rehydrate(
        OperationId id,
        OperationKey key,
        string name,
        OperationStatus status,
        decimal? managementCutPercent,
        IEnumerable<Guid> assignedOperatorIds,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        var operation = new Operation();
        operation.Apply(new OperationBackfilled(
            id.Value,
            key.Value,
            name,
            status.ToString(),
            managementCutPercent,
            assignedOperatorIds.ToArray(),
            createdAt,
            lastUpdatedAt));
        return operation;
    }

    public IResult TransitionTo(OperationStatus target)
    {
        if (Status == OperationStatus.Closed)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AlreadyClosed)
                .WithMessage("Operacao Encerrada e terminal e nao pode ser reaberta.")
                .Build());
        }

        var allowed = (Status, target) switch
        {
            (OperationStatus.Draft, OperationStatus.Active) => true,
            (OperationStatus.Draft, OperationStatus.Closed) => true,
            (OperationStatus.Active, OperationStatus.Paused) => true,
            (OperationStatus.Active, OperationStatus.Closed) => true,
            (OperationStatus.Active, OperationStatus.Draft) => true,
            (OperationStatus.Paused, OperationStatus.Active) => true,
            (OperationStatus.Paused, OperationStatus.Closed) => true,
            _ => false
        };

        if (!allowed || Status == target)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.InvalidTransition)
                .WithMessage($"Transicao {Status} -> {target} nao e permitida.")
                .Build());
        }

        var from = Status.ToString();
        ApplyChange(new OperationTransitioned(Id.Value, from, target.ToString(), DateTime.UtcNow, null));
        if (target == OperationStatus.Closed)
            ApplyChange(new OperationAssignmentsCleared(Id.Value, DateTime.UtcNow, null));

        return Result.Success();
    }

    public IResult ConfigureManagementCut(decimal? percent)
    {
        if (IsClosed)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AlreadyClosed)
                .WithMessage("Operacao Encerrada nao aceita configuracao.")
                .Build());
        }

        var cutCheck = ValidateCut(percent);
        if (cutCheck.IsFailure)
            return cutCheck;

        ApplyChange(new OperationManagementCutConfigured(Id.Value, percent, DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult AssignOperator(Guid memberId)
    {
        if (IsClosed)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AlreadyClosed)
                .WithMessage("Operacao Encerrada nao aceita assign.")
                .Build());
        }

        if (_assignedOperatorIds.Contains(memberId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AlreadyAssigned)
                .WithMessage("Operador ja esta assigned nesta Operacao.")
                .Build());
        }

        ApplyChange(new OperatorAssigned(Id.Value, memberId, DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult UnassignOperator(Guid memberId)
    {
        if (!_assignedOperatorIds.Contains(memberId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NotAssigned)
                .WithMessage("Operador nao esta assigned nesta Operacao.")
                .Build());
        }

        ApplyChange(new OperatorUnassigned(Id.Value, memberId, DateTime.UtcNow, null));
        return Result.Success();
    }

    public bool IsAssigned(Guid memberId) => _assignedOperatorIds.Contains(memberId);

    public void Apply(OperationOpened e)
    {
        Id = new OperationId(e.OperationId);
        Key = new OperationKey(e.Key);
        Name = e.Name;
        Status = OperationStatus.Draft;
        ManagementCutPercent = e.ManagementCutPercent;
        CreatedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(OperationBackfilled e)
    {
        Id = new OperationId(e.OperationId);
        Key = new OperationKey(e.Key);
        Name = e.Name;
        Status = Enum.TryParse<OperationStatus>(e.Status, true, out var status) ? status : OperationStatus.Draft;
        ManagementCutPercent = e.ManagementCutPercent;
        _assignedOperatorIds.Clear();
        foreach (var id in e.AssignedOperatorIds)
            _assignedOperatorIds.Add(id);
        CreatedAt = e.CreatedAt;
        LastUpdatedAt = e.LastUpdatedAt;
    }

    public void Apply(OperationTransitioned e)
    {
        Status = Enum.Parse<OperationStatus>(e.To, ignoreCase: true);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(OperationAssignmentsCleared e)
    {
        _assignedOperatorIds.Clear();
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(OperationManagementCutConfigured e)
    {
        ManagementCutPercent = e.ManagementCutPercent;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(OperatorAssigned e)
    {
        _assignedOperatorIds.Add(e.MemberId);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(OperatorUnassigned e)
    {
        _assignedOperatorIds.Remove(e.MemberId);
        LastUpdatedAt = e.OccurredAt;
    }

    private static IResult ValidateCut(decimal? percent)
    {
        if (percent is null)
            return Result.Success();

        if (percent < 0 || percent > 100)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.CutInvalid)
                .WithMessage("Cut de gestao deve ser null ou entre 0 e 100.")
                .Build());
        }

        return Result.Success();
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case OperationOpened e: Apply(e); break;
            case OperationBackfilled e: Apply(e); break;
            case OperationTransitioned e: Apply(e); break;
            case OperationAssignmentsCleared e: Apply(e); break;
            case OperationManagementCutConfigured e: Apply(e); break;
            case OperatorAssigned e: Apply(e); break;
            case OperatorUnassigned e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }

        _uncommitted.Add(@event);
    }
}

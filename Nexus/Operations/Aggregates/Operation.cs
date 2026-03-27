using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Aggregates;

public sealed class Operation
{
    public const int MaxNameLength = 200;

    private readonly List<string> _operatorIds;
    private readonly List<string> _strawManIds;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public IReadOnlyList<string> OperatorIds => _operatorIds.AsReadOnly();
    public IReadOnlyList<string> StrawManIds => _strawManIds.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal Operation(
        string Id,
        string Name,
        string? Description,
        IReadOnlyList<string> OperatorIds,
        IReadOnlyList<string> StrawManIds,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        _operatorIds = (OperatorIds ?? Array.Empty<string>())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        _strawManIds = (StrawManIds ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public IResult AddOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (_operatorIds.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorAlreadyAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is already assigned to this operation")
                .Build());

        _operatorIds.Add(normalizedOperatorId);
        Touch();

        return Result.Success();
    }

    public IResult RemoveOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();
        var removed = _operatorIds.Remove(normalizedOperatorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorNotAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AddStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID cannot be empty")
                .Build());

        var normalizedStrawManId = strawManId.Trim();

        if (_strawManIds.Contains(normalizedStrawManId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManAlreadyAssigned)
                .WithMessage($"Straw man '{normalizedStrawManId}' is already assigned to this operation")
                .Build());

        _strawManIds.Add(normalizedStrawManId);
        Touch();

        return Result.Success();
    }

    public IResult RemoveStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID cannot be empty")
                .Build());

        var normalizedStrawManId = strawManId.Trim();
        var removed = _strawManIds.Remove(normalizedStrawManId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManNotAssigned)
                .WithMessage($"Straw man '{normalizedStrawManId}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

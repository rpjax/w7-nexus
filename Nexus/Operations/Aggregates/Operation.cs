using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Aggregates;

public sealed class Operation
{
    private readonly List<string> _operators;

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<string> Operators => _operators.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal Operation(
        string id,
        string name,
        string description,
        IEnumerable<string> operators,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        _operators = (operators ?? Array.Empty<string>())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public IResult AddOperator(string operatorId)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID cannot be empty")
                .Build());

        var normalizedOperatorId = operatorId.Trim();

        if (_operators.Contains(normalizedOperatorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorAlreadyAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is already assigned to this operation")
                .Build());

        _operators.Add(normalizedOperatorId);
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
        var removed = _operators.Remove(normalizedOperatorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorNotAssigned)
                .WithMessage($"Operator '{normalizedOperatorId}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

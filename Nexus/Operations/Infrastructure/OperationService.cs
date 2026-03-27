using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.Application.Models;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Infrastructure;

public sealed class OperationService : IOperationService
{
    private readonly IOperationRepository _operations;

    public OperationService(IOperationRepository operations)
    {
        _operations = operations;
    }

    public async Task<IResult<Operation>> CreateOperationAsync(CreateOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name?.Trim();
        var description = request.Description?.Trim();
        var builder = Result.Create<Operation>();

        if (string.IsNullOrWhiteSpace(name))
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.NameInvalid)
                .WithMessage("Operation name is required")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > Operation.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.NameTooLong)
                .WithMessage($"Operation name must be at most {Operation.MaxNameLength} characters")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var normalizedName = name!.ToLowerInvariant();
        var nameTaken = _operations.AsQueryable()
            .Any(o => o.Name.ToLower() == normalizedName);
        if (nameTaken)
        {
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.NameAlreadyExists)
                .WithMessage($"Operation name '{name}' is already in use")
                .Build());
            return builder.Build();
        }

        var now = DateTime.UtcNow;
        var operation = new Operation(
            Id: Guid.NewGuid().ToString("N"),
            Name: name!,
            Description: string.IsNullOrWhiteSpace(description) ? null : description,
            OperatorIds: (request.Operators ?? Array.Empty<string>()).ToArray(),
            StrawManIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        await _operations.CreateAsync(operation);
        return builder.WithValue(operation).Build();
    }

    public async Task<IResult> AddOperatorAsync(string operationId, string operatorId)
    {
        var operation = await LoadOperationAsync(operationId);
        if (operation is null)
            return NotFoundResult(operationId);

        var result = operation.AddOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> RemoveOperatorAsync(string operationId, string operatorId)
    {
        var operation = await LoadOperationAsync(operationId);
        if (operation is null)
            return NotFoundResult(operationId);

        var result = operation.RemoveOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> AddStrawManAsync(string operationId, string strawManId)
    {
        var operation = await LoadOperationAsync(operationId);
        if (operation is null)
            return NotFoundResult(operationId);

        var result = operation.AddStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> RemoveStrawManAsync(string operationId, string strawManId)
    {
        var operation = await LoadOperationAsync(operationId);
        if (operation is null)
            return NotFoundResult(operationId);

        var result = operation.RemoveStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> DeleteOperationAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required")
                .Build());

        var operation = _operations.AsQueryable().FirstOrDefault(o => o.Id == operationId);
        if (operation is null)
            return NotFoundResult(operationId);

        await _operations.DeleteAsync(operation);
        return Result.Success();
    }

    private Task<Operation?> LoadOperationAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            return Task.FromResult<Operation?>(null);

        return Task.FromResult(_operations.AsQueryable().FirstOrDefault(o => o.Id == operationId));
    }

    private static IResult NotFoundResult(string operationId)
    {
        return Result.Failure(Error.Create()
            .WithCode(OperationErrorCodes.OperationNotFound)
            .WithMessage($"Operation '{operationId}' was not found")
            .Build());
    }
}

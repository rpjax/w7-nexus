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

        if (string.IsNullOrWhiteSpace(description))
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.DescriptionInvalid)
                .WithMessage("Operation description is required")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var now = DateTime.UtcNow;
        var operation = new Operation(
            id: Guid.NewGuid().ToString("N"),
            name: name!,
            description: description!,
            operators: request.Operators ?? Array.Empty<string>(),
            createdAt: now,
            updatedAt: now);

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

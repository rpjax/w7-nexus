using Aidan.Core.Errors;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operations.Aggregates;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Application;

public sealed class OperationService : IOperationService
{
    private readonly IOperationRepository _operations;
    private readonly IAccountIdValidator _accountIdValidator;

    public OperationService(
        IOperationRepository operations,
        IAccountIdValidator accountIdValidator)
    {
        _operations = operations;
        _accountIdValidator = accountIdValidator;
    }

    public async Task<IResult<Operation>> CreateOperationAsync(string? name, string? description)
    {
        var builder = Result.Create<Operation>();

        name = name?.Trim();
        description = description?.Trim();

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

        if (!string.IsNullOrWhiteSpace(description) && description.Length > Operation.MaxDescriptionLength)
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.DescriptionTooLong)
                .WithMessage($"Operation description must be at most {Operation.MaxDescriptionLength} characters")
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
            AdministratorIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        await _operations.CreateAsync(operation);

        return builder
            .WithValue(operation)
            .Build();
    }

    public async Task<IResult> AssignAdministratorAsync(string operationId, string administratorId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            administratorId,
            OperationErrorCodes.AdministratorInvalid,
            OperationErrorCodes.AdministratorAccountNotFound,
            "Administrator");
        if (accountValidation is not null)
            return accountValidation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignAdministrator(administratorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> UnassignAdministratorAsync(string operationId, string administratorId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        if (string.IsNullOrWhiteSpace(administratorId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorInvalid)
                .WithMessage("Administrator ID is required")
                .Build());
        }

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignAdministrator(administratorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> DeleteOperationAsync(string operationId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        await _operations.DeleteAsync(operation);
        return Result.Success();
    }

    private Operation? FindOperation(string normalizedOperationId)
        => _operations.AsQueryable().FirstOrDefault(o => o.Id == normalizedOperationId);

    private async Task<IResult?> ValidateAccountExistsAsync(
        string? accountId,
        string invalidCode,
        string notFoundCode,
        string label)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage($"{label} ID cannot be empty")
                .Build());
        }

        var normalizedAccountId = accountId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedAccountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"{label} account '{normalizedAccountId}' was not found")
                .Build());
        }

        return null;
    }

    private static IResult? ValidateOperationId(string? operationId, out string normalizedOperationId)
    {
        normalizedOperationId = operationId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedOperationId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string operationId)
    {
        return Result.Failure(Error.Create()
            .WithCode(OperationErrorCodes.OperationNotFound)
            .WithMessage($"Operation '{operationId}' was not found")
            .Build());
    }
}

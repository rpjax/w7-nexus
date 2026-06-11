using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application;
using Nexus.Actors.Responses.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Infrastructure;

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

    public async Task<IResult<OperationDetails>> CreateOperationAsync(
        string? name,
        string? description,
        string[] operatorIds)
    {
        var builder = Result.Create<OperationDetails>();

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

        var normalizedOperatorIds = NormalizeAccountIds(operatorIds);
        foreach (var operatorId in normalizedOperatorIds)
        {
            if (!await _accountIdValidator.ExistsAsync(operatorId))
            {
                builder.WithError(Error.Create()
                    .WithCode(OperationErrorCodes.OperatorAccountNotFound)
                    .WithMessage($"Operator account '{operatorId}' was not found")
                    .Build());
            }
        }

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
            OperatorIds: normalizedOperatorIds,
            StrawManIds: Array.Empty<string>(),
            ManuallySetChargeCredentials: false,
            ChargeCredentialsIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        await _operations.CreateAsync(operation);

        var operationDetails = OperationDetails.FromOperation(operation);

        return builder
            .WithValue(operationDetails)
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

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignAdministrator(administratorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> AssignOperatorAsync(string operationId, string operatorId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            operatorId,
            OperationErrorCodes.OperatorInvalid,
            OperationErrorCodes.OperatorAccountNotFound,
            "Operator");
        if (accountValidation is not null)
            return accountValidation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> UnassignOperatorAsync(string operationId, string operatorId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignOperator(operatorId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> AssignStrawManAsync(string operationId, string strawManId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            strawManId,
            OperationErrorCodes.StrawManInvalid,
            OperationErrorCodes.StrawManAccountNotFound,
            "Straw man");
        if (accountValidation is not null)
            return accountValidation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> UnassignStrawManAsync(string operationId, string strawManId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignStrawMan(strawManId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> SetGatewaySelectionStrategyAsync(
        string operationId,
        OperationGatewaySelectionStrategy strategy)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.SetGatewaySelectionStrategy(strategy);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> AssignGatewayCredentialsGroupAsync(string operationId, string groupId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignGatewayCredentialsGroup(groupId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> UnassignGatewayCredentialsGroupAsync(string operationId, string groupId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignGatewayCredentialsGroup(groupId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> AssignGatewayCredentialsAsync(string operationId, string credentialsId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignGatewayCredentials(credentialsId);
        if (result.IsFailure)
            return result;

        await _operations.UpdateAsync(operation);
        return Result.Success();
    }

    public async Task<IResult> UnassignGatewayCredentialsAsync(string operationId, string credentialsId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignGatewayCredentials(credentialsId);
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

    private static string[] NormalizeAccountIds(string[]? accountIds)
        => (accountIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

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

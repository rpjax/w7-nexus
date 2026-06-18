using Aidan.Core.Errors;
using Nexus.Gateways.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Application.Services;

public sealed class OperationService : IOperationService
{
    private readonly IOperationRepository _operations;
    private readonly IAccountIdValidator _accountIdValidator;
    private readonly IGatewayCredentialsGroupRepository _gatewayCredentialsGroups;
    private readonly IGatewayCredentialsIdValidator _gatewayCredentialsIdValidator;

    public OperationService(
        IOperationRepository operations,
        IAccountIdValidator accountIdValidator,
        IGatewayCredentialsGroupRepository gatewayCredentialsGroups,
        IGatewayCredentialsIdValidator gatewayCredentialsIdValidator)
    {
        _operations = operations;
        _accountIdValidator = accountIdValidator;
        _gatewayCredentialsGroups = gatewayCredentialsGroups;
        _gatewayCredentialsIdValidator = gatewayCredentialsIdValidator;
    }

    public async Task<IResult<Operation>> CreateOperationAsync(string? name, string? description)
    {
        var builder = Result.Create<Operation>();

        name = name?.Trim();
        description = description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.NameInvalid)
                .WithMessage("O nome da operação é obrigatório.")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > Operation.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.NameTooLong)
                .WithMessage($"O nome da operação pode ter no máximo {Operation.MaxNameLength} caracteres.")
                .Build());

        if (!string.IsNullOrWhiteSpace(description) && description.Length > Operation.MaxDescriptionLength)
            builder.WithError(Error.Create()
                .WithCode(OperationErrorCodes.DescriptionTooLong)
                .WithMessage($"A descrição da operação pode ter no máximo {Operation.MaxDescriptionLength} caracteres.")
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
                .WithMessage($"O nome de operação '{name}' já está em uso. Escolha outro nome.")
                .Build());
            return builder.Build();
        }

        var now = DateTime.UtcNow;
        var operation = new Operation(
            Id: string.Empty,
            Name: name!,
            Description: string.IsNullOrWhiteSpace(description) ? null : description,
            AdministratorIds: Array.Empty<string>(),
            StrawManIds: Array.Empty<string>(),
            GatewaySelectionStrategy: GatewaySelectionStrategy.PerStrawman,
            GatewayCredentialsIds: Array.Empty<string>(),
            GatewayCredentialsGroupIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        operation = await _operations.CreateAsync(operation);

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
            "administrador");
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
                .WithMessage("O ID do administrador é obrigatório.")
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

    public async Task<IResult> AssignStrawManAsync(string operationId, string strawManId)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        var accountValidation = await ValidateAccountExistsAsync(
            strawManId,
            OperationErrorCodes.StrawManInvalid,
            OperationErrorCodes.StrawManAccountNotFound,
            "laranja");
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

    public async Task<IResult> SetGatewaySelectionStrategyAsync(string operationId, GatewaySelectionStrategy strategy)
    {
        var validation = ValidateOperationId(operationId, out var normalizedOperationId);
        if (validation is not null)
            return validation;

        if (!Enum.IsDefined(strategy))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewaySelectionStrategyInvalid)
                .WithMessage("A estratégia de seleção de gateway é inválida.")
                .Build());
        }

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

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais é obrigatório.")
                .Build());
        }

        var normalizedGroupId = groupId.Trim();
        var groupExists = _gatewayCredentialsGroups.AsQueryable()
            .Any(g => g.Id == normalizedGroupId);
        if (!groupExists)
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupNotFound)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' não foi encontrado.")
                .Build());
        }

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignGatewayCredentialsGroup(normalizedGroupId);
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

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais é obrigatório.")
                .Build());
        }

        var normalizedGroupId = groupId.Trim();

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.UnassignGatewayCredentialsGroup(normalizedGroupId);
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

        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway é obrigatório.")
                .Build());
        }

        var normalizedCredentialsId = credentialsId.Trim();
        if (!await _gatewayCredentialsIdValidator.ExistsAsync(normalizedCredentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialInvalid)
                .WithMessage($"A credencial de gateway '{normalizedCredentialsId}' não foi encontrada.")
                .Build());
        }

        var operation = FindOperation(normalizedOperationId);
        if (operation is null)
            return NotFoundResult(normalizedOperationId);

        var result = operation.AssignGatewayCredentials(normalizedCredentialsId);
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
                .WithMessage($"O ID de {label} não pode estar vazio.")
                .Build());
        }

        var normalizedAccountId = accountId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedAccountId))
        {
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"A conta de {label} '{normalizedAccountId}' não foi encontrada.")
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
                .WithMessage("O ID da operação é obrigatório.")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string operationId)
    {
        return Result.Failure(Error.Create()
            .WithCode(OperationErrorCodes.OperationNotFound)
            .WithMessage($"A operação '{operationId}' não foi encontrada.")
            .Build());
    }
}

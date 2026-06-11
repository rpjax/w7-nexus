using Aidan.Core.Errors;
using Nexus.Gateways.Application.Services.Contracts;
using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Errors;

namespace Nexus.Gateways.Application.Services;

public sealed class GatewayCredentialsGroupService : IGatewayCredentialsGroupService
{
    private readonly IGatewayCredentialsGroupRepository _groups;
    private readonly IGatewayCredentialsIdValidator _credentialsIdValidator;

    public GatewayCredentialsGroupService(
        IGatewayCredentialsGroupRepository groups,
        IGatewayCredentialsIdValidator credentialsIdValidator)
    {
        _groups = groups;
        _credentialsIdValidator = credentialsIdValidator;
    }

    public async Task<IResult<GatewayCredentialsGroupDetails>> CreateGroupAsync(string name)
    {
        var builder = Result.Create<GatewayCredentialsGroupDetails>();

        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            builder.WithError(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.NameInvalid)
                .WithMessage("O nome do grupo é obrigatório.")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > GatewayCredentialsGroup.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.NameTooLong)
                .WithMessage($"O nome do grupo pode ter no máximo {GatewayCredentialsGroup.MaxNameLength} caracteres.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var normalizedName = name.ToLowerInvariant();
        var nameTaken = _groups.AsQueryable()
            .Any(g => g.Name.ToLower() == normalizedName);
        if (nameTaken)
        {
            builder.WithError(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.NameAlreadyExists)
                .WithMessage($"O nome de grupo '{name}' já está em uso. Escolha outro nome.")
                .Build());
            return builder.Build();
        }

        var now = DateTime.UtcNow;
        var group = new GatewayCredentialsGroup(
            Id: string.Empty,
            Name: name,
            GatewayCredentialsIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        group = await _groups.CreateAsync(group);

        return builder
            .WithValue(GatewayCredentialsGroupDetails.FromGroup(group))
            .Build();
    }

    public async Task<IResult> AssignGatewayCredentialsAsync(string groupId, string credentialsId)
    {
        var validation = ValidateGroupId(groupId, out var normalizedGroupId);
        if (validation is not null)
            return validation;

        var credentialValidation = await ValidateCredentialExistsAsync(credentialsId);
        if (credentialValidation is not null)
            return credentialValidation;

        var group = FindGroup(normalizedGroupId);
        if (group is null)
            return NotFoundResult(normalizedGroupId);

        var result = group.AssignGatewayCredentials(credentialsId);
        if (result.IsFailure)
            return result;

        await _groups.UpdateAsync(group);
        return Result.Success();
    }

    public async Task<IResult> UnassignGatewayCredentialsAsync(string groupId, string credentialsId)
    {
        var validation = ValidateGroupId(groupId, out var normalizedGroupId);
        if (validation is not null)
            return validation;

        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway é obrigatório.")
                .Build());
        }

        var group = FindGroup(normalizedGroupId);
        if (group is null)
            return NotFoundResult(normalizedGroupId);

        var result = group.UnassignGatewayCredentials(credentialsId);
        if (result.IsFailure)
            return result;

        await _groups.UpdateAsync(group);
        return Result.Success();
    }

    public async Task<IResult> DeleteGroupAsync(string groupId)
    {
        var validation = ValidateGroupId(groupId, out var normalizedGroupId);
        if (validation is not null)
            return validation;

        var group = FindGroup(normalizedGroupId);
        if (group is null)
            return NotFoundResult(normalizedGroupId);

        await _groups.DeleteAsync(group);
        return Result.Success();
    }

    private GatewayCredentialsGroup? FindGroup(string normalizedGroupId)
        => _groups.AsQueryable().FirstOrDefault(g => g.Id == normalizedGroupId);

    private async Task<IResult?> ValidateCredentialExistsAsync(string? credentialsId)
    {
        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway é obrigatório.")
                .Build());
        }

        var normalized = credentialsId.Trim();
        if (!await _credentialsIdValidator.ExistsAsync(normalized))
        {
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialNotFound)
                .WithMessage($"A credencial de gateway '{normalized}' não foi encontrada.")
                .Build());
        }

        return null;
    }

    private static IResult? ValidateGroupId(string? groupId, out string normalizedGroupId)
    {
        normalizedGroupId = groupId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedGroupId))
        {
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GroupIdInvalid)
                .WithMessage("O ID do grupo é obrigatório.")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string groupId)
    {
        return Result.Failure(Error.Create()
            .WithCode(GatewayCredentialsGroupErrorCodes.GroupNotFound)
            .WithMessage($"O grupo de credenciais '{groupId}' não foi encontrado.")
            .Build());
    }
}

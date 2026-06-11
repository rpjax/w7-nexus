using Aidan.Core.Errors;
using Nexus.Gateways.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Entities;
using Nexus.Gateways.ErrorCodes;

namespace Nexus.Gateways.Application;

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
                .WithMessage("Group name is required")
                .Build());

        if (!string.IsNullOrWhiteSpace(name) && name.Length > GatewayCredentialsGroup.MaxNameLength)
            builder.WithError(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.NameTooLong)
                .WithMessage($"Group name must be at most {GatewayCredentialsGroup.MaxNameLength} characters")
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
                .WithMessage($"Group name '{name}' is already in use")
                .Build());
            return builder.Build();
        }

        var now = DateTime.UtcNow;
        var group = new GatewayCredentialsGroup(
            Id: Guid.NewGuid().ToString("N"),
            Name: name,
            GatewayCredentialsIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);

        await _groups.CreateAsync(group);

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
                .WithMessage("Gateway credential ID is required")
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
                .WithMessage("Gateway credential ID is required")
                .Build());
        }

        var normalized = credentialsId.Trim();
        if (!await _credentialsIdValidator.ExistsAsync(normalized))
        {
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialNotFound)
                .WithMessage($"Gateway credential '{normalized}' was not found")
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
                .WithMessage("Group ID is required")
                .Build());
        }

        return null;
    }

    private static IResult NotFoundResult(string groupId)
    {
        return Result.Failure(Error.Create()
            .WithCode(GatewayCredentialsGroupErrorCodes.GroupNotFound)
            .WithMessage($"Gateway credentials group '{groupId}' was not found")
            .Build());
    }
}

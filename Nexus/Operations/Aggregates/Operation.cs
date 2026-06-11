using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.ErrorCodes;

namespace Nexus.Operations.Aggregates;

public enum OperationGatewaySelectionStrategy
{
    PerStrawman,
    PerGroup,
    Manual
}

public sealed class Operation
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    private readonly List<string> _administratorIds;
    private readonly List<string> _operatorIds;
    private readonly List<string> _strawManIds;
    private readonly List<string> _gatewayCredentialsGroupIds;
    private readonly List<string> _gatewayCredentialsIds;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public OperationGatewaySelectionStrategy GatewaySelectionStrategy { get; private set; }
    public IReadOnlyList<string> AdministratorIds => _administratorIds.AsReadOnly();
    public IReadOnlyList<string> OperatorIds => _operatorIds.AsReadOnly();
    public IReadOnlyList<string> StrawManIds => _strawManIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsGroupIds => _gatewayCredentialsGroupIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsIds => _gatewayCredentialsIds.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public bool ManuallySetChargeCredentials => GatewaySelectionStrategy == OperationGatewaySelectionStrategy.Manual;

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
        bool ManuallySetChargeCredentials,
        IReadOnlyList<string> ChargeCredentialsIds,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        _administratorIds = new List<string>();
        _operatorIds = NormalizeIds(OperatorIds);
        _strawManIds = NormalizeIds(StrawManIds);
        GatewaySelectionStrategy = ManuallySetChargeCredentials
            ? OperationGatewaySelectionStrategy.Manual
            : OperationGatewaySelectionStrategy.PerStrawman;
        _gatewayCredentialsGroupIds = new List<string>();
        _gatewayCredentialsIds = NormalizeIds(ChargeCredentialsIds);
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public IResult AssignAdministrator(string administratorId)
    {
        if (string.IsNullOrWhiteSpace(administratorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorInvalid)
                .WithMessage("Administrator ID cannot be empty")
                .Build());

        var normalizedAdministratorId = administratorId.Trim();

        if (_administratorIds.Contains(normalizedAdministratorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorAlreadyAssigned)
                .WithMessage($"Administrator '{normalizedAdministratorId}' is already assigned to this operation")
                .Build());

        _administratorIds.Add(normalizedAdministratorId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignAdministrator(string administratorId)
    {
        if (string.IsNullOrWhiteSpace(administratorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorInvalid)
                .WithMessage("Administrator ID cannot be empty")
                .Build());

        var normalizedAdministratorId = administratorId.Trim();
        var removed = _administratorIds.Remove(normalizedAdministratorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorNotAssigned)
                .WithMessage($"Administrator '{normalizedAdministratorId}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignOperator(string operatorId)
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

    public IResult UnassignOperator(string operatorId)
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

    public IResult SetGatewaySelectionStrategy(OperationGatewaySelectionStrategy strategy)
    {
        if (GatewaySelectionStrategy == strategy)
            return Result.Success();

        GatewaySelectionStrategy = strategy;
        Touch();
        return Result.Success();
    }

    public IResult AssignStrawMan(string strawManId)
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

    public IResult UnassignStrawMan(string strawManId)
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

    public IResult AssignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != OperationGatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Gateway credential groups can only be assigned when the selection strategy is PerGroup")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID cannot be empty")
                .Build());

        var normalizedGroupId = groupId.Trim();

        if (_gatewayCredentialsGroupIds.Contains(normalizedGroupId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupAlreadyAssigned)
                .WithMessage($"Gateway credentials group '{normalizedGroupId}' is already assigned to this operation")
                .Build());

        _gatewayCredentialsGroupIds.Add(normalizedGroupId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != OperationGatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Gateway credential groups can only be unassigned when the selection strategy is PerGroup")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID cannot be empty")
                .Build());

        var normalizedGroupId = groupId.Trim();
        var removed = _gatewayCredentialsGroupIds.Remove(normalizedGroupId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupNotAssigned)
                .WithMessage($"Gateway credentials group '{normalizedGroupId}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != OperationGatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ManualChargeCredentialsDisabled)
                .WithMessage("Manual gateway credential selection is not enabled for this operation")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();

        if (_gatewayCredentialsIds.Contains(normalized, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialAlreadyAssigned)
                .WithMessage($"Gateway credential '{normalized}' is already assigned to this operation")
                .Build());

        _gatewayCredentialsIds.Add(normalized);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != OperationGatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ManualChargeCredentialsDisabled)
                .WithMessage("Manual gateway credential selection is not enabled for this operation")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();
        var removed = _gatewayCredentialsIds.Remove(normalized);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialNotAssigned)
                .WithMessage($"Gateway credential '{normalized}' is not assigned to this operation")
                .Build());

        Touch();

        return Result.Success();
    }

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids)
        => (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

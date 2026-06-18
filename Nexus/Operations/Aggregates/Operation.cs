using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Aggregates;

public sealed class Operation : IGatewayCredentialScope
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    private readonly List<string> _administratorIds;
    private readonly List<string> _strawManIds;
    private readonly List<string> _gatewayCredentialsGroupIds;
    private readonly List<string> _gatewayCredentialsIds;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public IReadOnlyList<string> AdministratorIds => _administratorIds.AsReadOnly();
    public GatewaySelectionStrategy GatewaySelectionStrategy { get; private set; }
    public IReadOnlyList<string> StrawManIds => _strawManIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsGroupIds => _gatewayCredentialsGroupIds.AsReadOnly();
    public IReadOnlyList<string> GatewayCredentialsIds => _gatewayCredentialsIds.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public bool ManuallySetGatewayCredentials => GatewaySelectionStrategy == GatewaySelectionStrategy.Manual;

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal Operation(
        string Id,
        string Name,
        string? Description,
        IReadOnlyList<string> AdministratorIds,
        IReadOnlyList<string> StrawManIds,
        GatewaySelectionStrategy GatewaySelectionStrategy,
        IReadOnlyList<string> GatewayCredentialsIds,
        IReadOnlyList<string> GatewayCredentialsGroupIds,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id.Trim();
        this.Name = Name.Trim();
        this.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        _administratorIds = NormalizeIds(AdministratorIds);
        _strawManIds = NormalizeIds(StrawManIds);
        this.GatewaySelectionStrategy = GatewaySelectionStrategy;
        _gatewayCredentialsGroupIds = NormalizeIds(GatewayCredentialsGroupIds);
        _gatewayCredentialsIds = NormalizeIds(GatewayCredentialsIds);
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public IResult AssignAdministrator(string administratorId)
    {
        if (string.IsNullOrWhiteSpace(administratorId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorInvalid)
                .WithMessage("O ID do administrador não pode estar vazio.")
                .Build());

        var normalizedAdministratorId = administratorId.Trim();

        if (_administratorIds.Contains(normalizedAdministratorId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorAlreadyAssigned)
                .WithMessage($"O administrador '{normalizedAdministratorId}' já está atribuído a esta operação.")
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
                .WithMessage("O ID do administrador não pode estar vazio.")
                .Build());

        var normalizedAdministratorId = administratorId.Trim();
        var removed = _administratorIds.Remove(normalizedAdministratorId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.AdministratorNotAssigned)
                .WithMessage($"O administrador '{normalizedAdministratorId}' não está atribuído a esta operação.")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult SetGatewaySelectionStrategy(GatewaySelectionStrategy strategy)
    {
        if (!Enum.IsDefined(strategy))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewaySelectionStrategyInvalid)
                .WithMessage("A estratégia de seleção de gateway é inválida.")
                .Build());

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
                .WithMessage("O ID do laranja não pode estar vazio.")
                .Build());

        var normalizedStrawManId = strawManId.Trim();

        if (_strawManIds.Contains(normalizedStrawManId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManAlreadyAssigned)
                .WithMessage($"O laranja '{normalizedStrawManId}' já está atribuído a esta operação.")
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
                .WithMessage("O ID do laranja não pode estar vazio.")
                .Build());

        var normalizedStrawManId = strawManId.Trim();
        var removed = _strawManIds.Remove(normalizedStrawManId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StrawManNotAssigned)
                .WithMessage($"O laranja '{normalizedStrawManId}' não está atribuído a esta operação.")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Grupos de credenciais só podem ser atribuídos quando a estratégia de seleção é Por Grupo.")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais não pode estar vazio.")
                .Build());

        var normalizedGroupId = groupId.Trim();

        if (_gatewayCredentialsGroupIds.Contains(normalizedGroupId, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupAlreadyAssigned)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' já está atribuído a esta operação.")
                .Build());

        _gatewayCredentialsGroupIds.Add(normalizedGroupId);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentialsGroup(string groupId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.PerGroup)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupStrategyMismatch)
                .WithMessage("Grupos de credenciais só podem ser removidos quando a estratégia de seleção é Por Grupo.")
                .Build());

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("O ID do grupo de credenciais não pode estar vazio.")
                .Build());

        var normalizedGroupId = groupId.Trim();
        var removed = _gatewayCredentialsGroupIds.Remove(normalizedGroupId);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupNotAssigned)
                .WithMessage($"O grupo de credenciais '{normalizedGroupId}' não está atribuído a esta operação.")
                .Build());

        Touch();

        return Result.Success();
    }

    public IResult AssignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ManualGatewayCredentialsDisabled)
                .WithMessage("A seleção manual de credenciais de gateway não está habilitada para esta operação.")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway não pode estar vazio.")
                .Build());

        var normalized = credentialsId.Trim();

        if (_gatewayCredentialsIds.Contains(normalized, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialAlreadyAssigned)
                .WithMessage($"A credencial de gateway '{normalized}' já está atribuída a esta operação.")
                .Build());

        _gatewayCredentialsIds.Add(normalized);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentials(string credentialsId)
    {
        if (GatewaySelectionStrategy != GatewaySelectionStrategy.Manual)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ManualGatewayCredentialsDisabled)
                .WithMessage("A seleção manual de credenciais de gateway não está habilitada para esta operação.")
                .Build());

        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialInvalid)
                .WithMessage("O ID da credencial de gateway não pode estar vazio.")
                .Build());

        var normalized = credentialsId.Trim();
        var removed = _gatewayCredentialsIds.Remove(normalized);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialNotAssigned)
                .WithMessage($"A credencial de gateway '{normalized}' não está atribuída a esta operação.")
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

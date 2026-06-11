using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Charges.ErrorCodes;

namespace Nexus.Charges.Entities;

public sealed class GatewayCredentialsGroup
{
    public const int MaxNameLength = 200;

    private readonly List<string> _gatewayCredentialsIds;

    public string Id { get; }
    public string Name { get; }
    public IReadOnlyList<string> GatewayCredentialsIds => _gatewayCredentialsIds.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal GatewayCredentialsGroup(
        string Id,
        string Name,
        IReadOnlyList<string> GatewayCredentialsIds,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id.Trim();
        this.Name = Name.Trim();
        _gatewayCredentialsIds = NormalizeIds(GatewayCredentialsIds);
        CreatedAt = CreatedAt;
        UpdatedAt = UpdatedAt;
    }

    public IResult AssignGatewayCredentials(string credentialsId)
    {
        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();

        if (_gatewayCredentialsIds.Contains(normalized, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialAlreadyAssigned)
                .WithMessage($"Gateway credential '{normalized}' is already assigned to this group")
                .Build());

        _gatewayCredentialsIds.Add(normalized);
        Touch();

        return Result.Success();
    }

    public IResult UnassignGatewayCredentials(string credentialsId)
    {
        if (string.IsNullOrWhiteSpace(credentialsId))
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid)
                .WithMessage("Gateway credential ID cannot be empty")
                .Build());

        var normalized = credentialsId.Trim();
        var removed = _gatewayCredentialsIds.Remove(normalized);

        if (!removed)
            return Result.Failure(Error.Create()
                .WithCode(GatewayCredentialsGroupErrorCodes.GatewayCredentialNotAssigned)
                .WithMessage($"Gateway credential '{normalized}' is not assigned to this group")
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

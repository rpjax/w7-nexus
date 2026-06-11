using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Operations.Errors;

namespace Nexus.Operations.Aggregates;

public sealed class Operation
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    private readonly List<string> _administratorIds;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public IReadOnlyList<string> AdministratorIds => _administratorIds.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// LINQCOMPATIBLE constructor used by repository projections/rehydration.
    /// Keep this signature simple and stable for LINQ providers.
    /// </summary>
    internal Operation(
        string Id,
        string Name,
        string? Description,
        IReadOnlyList<string> AdministratorIds,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id.Trim();
        this.Name = Name.Trim();
        this.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        _administratorIds = NormalizeIds(AdministratorIds);
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

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids)
        => (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

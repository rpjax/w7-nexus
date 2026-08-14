using System.Text.Json;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

public enum MandateScopeKind
{
    Organization = 0,
    CarteiraDirect = 1,
    OperationNone = 2,
    OperationAll = 3,
    OperationSpecific = 4
}

public sealed class MandateScope : IEquatable<MandateScope>
{
    private MandateScope(MandateScopeKind kind, IReadOnlyList<Guid> operationIds)
    {
        Kind = kind;
        OperationIds = operationIds;
    }

    public MandateScopeKind Kind { get; }
    public IReadOnlyList<Guid> OperationIds { get; }

    public static MandateScope Organization() => new(MandateScopeKind.Organization, []);
    public static MandateScope CarteiraDirect() => new(MandateScopeKind.CarteiraDirect, []);
    public static MandateScope OperationNone() => new(MandateScopeKind.OperationNone, []);
    public static MandateScope OperationAll() => new(MandateScopeKind.OperationAll, []);

    public static IResult<MandateScope> OperationSpecific(IEnumerable<Guid> operationIds)
    {
        var ids = operationIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Result<MandateScope>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityEmpty)
                .WithMessage("OperationSpecific exige ao menos um operation id.")
                .Build());
        }

        return Result<MandateScope>.Success(new MandateScope(MandateScopeKind.OperationSpecific, ids));
    }

    public bool Covers(MandateScope requested)
    {
        if (Kind == MandateScopeKind.Organization)
            return true;

        if (Kind == MandateScopeKind.OperationAll
            && requested.Kind is MandateScopeKind.OperationAll
                or MandateScopeKind.OperationNone
                or MandateScopeKind.OperationSpecific)
        {
            return true;
        }

        if (Kind != requested.Kind)
            return false;

        if (Kind != MandateScopeKind.OperationSpecific)
            return true;

        return requested.OperationIds.All(id => OperationIds.Contains(id));
    }

    public string ToStorageJson() => JsonSerializer.Serialize(new StorageDto
    {
        Kind = Kind.ToString(),
        OperationIds = OperationIds.ToArray()
    });

    public static MandateScope FromStorageJson(string json)
    {
        var dto = JsonSerializer.Deserialize<StorageDto>(json)
            ?? throw new InvalidOperationException("Invalid mandate scope JSON.");

        if (!Enum.TryParse<MandateScopeKind>(dto.Kind, ignoreCase: true, out var kind))
            throw new InvalidOperationException($"Unknown mandate scope kind '{dto.Kind}'.");

        return new MandateScope(kind, dto.OperationIds ?? []);
    }

    public bool Equals(MandateScope? other)
    {
        if (other is null) return false;
        if (Kind != other.Kind) return false;
        if (OperationIds.Count != other.OperationIds.Count) return false;
        return OperationIds.SequenceEqual(other.OperationIds);
    }

    public override bool Equals(object? obj) => obj is MandateScope other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        foreach (var id in OperationIds)
            hash.Add(id);
        return hash.ToHashCode();
    }

    private sealed class StorageDto
    {
        public string Kind { get; set; } = string.Empty;
        public Guid[]? OperationIds { get; set; }
    }
}

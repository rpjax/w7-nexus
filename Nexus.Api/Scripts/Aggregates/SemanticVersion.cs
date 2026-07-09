namespace Nexus.Scripts.Aggregates;

public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public SemanticVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static bool TryParse(string? input, out SemanticVersion? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var parts = input.Trim().Split('.');

        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || !int.TryParse(parts[2], out var patch))
            return false;

        if (major < 0 || minor < 0 || patch < 0)
            return false;

        version = new SemanticVersion(major, minor, patch);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
            return 1;

        var major = Major.CompareTo(other.Major);
        if (major != 0)
            return major;

        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0)
            return minor;

        return Patch.CompareTo(other.Patch);
    }

    public SemanticVersion NextPatch() => new(Major, Minor, Patch + 1);

    public bool Equals(SemanticVersion? other) =>
        other is not null
        && Major == other.Major
        && Minor == other.Minor
        && Patch == other.Patch;

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

namespace Nexus.Scripts.Aggregates;

public sealed class Channel
{
    public string Id { get; }
    public ChannelKey Key { get; }
    public string? CurrentReleaseId { get; private set; }

    internal Channel(string id, ChannelKey key, string? currentReleaseId)
    {
        Id = id;
        Key = key;
        CurrentReleaseId = currentReleaseId;
    }

    internal static Channel CreateDefault(ChannelKey key) =>
        new(string.Empty, key, currentReleaseId: null);

    internal void Promote(string releaseId) => CurrentReleaseId = releaseId;

    internal void ClearRelease() => CurrentReleaseId = null;
}

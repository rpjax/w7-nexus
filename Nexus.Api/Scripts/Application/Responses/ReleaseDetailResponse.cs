namespace Nexus.Scripts.Application.Responses;

public sealed class ReleaseDetailResponse
{
    public string Id { get; init; } = string.Empty;
    public string ScriptId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public int SourceCodeSizeBytes { get; init; }
    public bool IsDeprecated { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> PromotedChannelRouteValues { get; init; } = Array.Empty<string>();
}

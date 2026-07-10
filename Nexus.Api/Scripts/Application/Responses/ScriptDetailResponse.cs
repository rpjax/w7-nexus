namespace Nexus.Scripts.Application.Responses;

public sealed class ScriptDetailResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string[] HostPatterns { get; init; } = Array.Empty<string>();
    public int Priority { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ChannelSummary> Channels { get; init; } = new();
}

public sealed class ChannelSummary
{
    public string RouteValue { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
    public string? CurrentReleaseId { get; init; }
    public string? Version { get; init; }
    public string? Hash { get; init; }
    public bool? IsDeprecated { get; init; }
}

namespace Nexus.Scripts.Application.Responses;

public sealed class SearchScriptsResponse
{
    public int Offset { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
    public List<ScriptSummary> Items { get; init; } = new();
}

public sealed class ScriptSummary
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

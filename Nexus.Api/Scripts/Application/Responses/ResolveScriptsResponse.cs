namespace Nexus.Scripts.Application.Responses;

public sealed class ResolveScriptsResponse
{
    public List<ResolvedScriptItem> Items { get; init; } = new();
    public string AggregateHash { get; init; } = string.Empty;
}

public sealed class ResolvedScriptItem
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public string SourceCode { get; init; } = string.Empty;
    public int Priority { get; init; }
}

namespace Nexus.Scripts.Application.Requests;

public sealed class SearchScriptsRequest
{
    public int Limit { get; init; } = 20;
    public int Offset { get; init; }
    public string? Keyword { get; init; }
}

namespace Nexus.Scripts.Application.Requests;

public sealed class CreateScriptRequest
{
    public string Name { get; init; } = string.Empty;
    public string[]? HostPatterns { get; init; }
    public int Priority { get; init; }
    public string? Description { get; init; }
}

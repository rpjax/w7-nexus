namespace Nexus.Scripts.Application.Requests;

public sealed class ResolveScriptsRequest
{
    public string? Host { get; init; }
    public string? Name { get; init; }
    public string? Channel { get; init; }
    public bool AllowDeprecated { get; init; }
    public string? Version { get; init; }
}

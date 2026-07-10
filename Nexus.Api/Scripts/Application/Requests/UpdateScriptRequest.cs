namespace Nexus.Scripts.Application.Requests;

public sealed class UpdateScriptRequest
{
    public int? Priority { get; init; }
    public string? Description { get; init; }
    public string[]? HostPatterns { get; init; }
}

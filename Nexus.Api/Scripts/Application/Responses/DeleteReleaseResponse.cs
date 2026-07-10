namespace Nexus.Scripts.Application.Responses;

public sealed class DeleteReleaseResponse
{
    public IReadOnlyList<string> ClearedChannelRouteValues { get; init; } = Array.Empty<string>();
}

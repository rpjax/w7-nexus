namespace Nexus.Scripts.Application.Responses;

public sealed class PublishReleaseResponse
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public int SourceCodeSizeBytes { get; init; }
}

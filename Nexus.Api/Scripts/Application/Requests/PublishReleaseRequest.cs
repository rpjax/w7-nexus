namespace Nexus.Scripts.Application.Requests;

public sealed class PublishReleaseRequest
{
    public string SourceCode { get; init; } = string.Empty;
    public int? Major { get; init; }
    public int? Minor { get; init; }
    public int? Patch { get; init; }
}

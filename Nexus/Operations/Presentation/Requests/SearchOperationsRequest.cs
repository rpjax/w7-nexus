namespace Nexus.Operations.Presentation.Requests;

public class SearchOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

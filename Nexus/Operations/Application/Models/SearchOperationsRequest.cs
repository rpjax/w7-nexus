namespace Nexus.Operations.Application.Models;

public class SearchOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

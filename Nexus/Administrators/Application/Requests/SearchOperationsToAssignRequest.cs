namespace Nexus.Administrators.Application.Requests;

public class SearchOperationsToAssignRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

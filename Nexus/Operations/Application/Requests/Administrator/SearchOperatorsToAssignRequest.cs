namespace Nexus.Operations.Application.Requests.Administrator;

public class SearchOperatorsToAssignRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

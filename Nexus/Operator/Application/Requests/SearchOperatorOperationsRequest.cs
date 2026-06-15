namespace Nexus.Operator.Application.Requests;

public class SearchOperatorOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

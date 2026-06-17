namespace Nexus.OperationAdministrators.Application.Requests;

public class SearchStrawMenToAssignRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

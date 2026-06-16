namespace Nexus.OperationAdministrator.Application.Requests;

public class SearchOperationAdministratorOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

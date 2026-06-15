namespace Nexus.Administrator.Application.Requests;

public class SearchOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public string[] AdministratorIds { get; set; } = new string[0];
}

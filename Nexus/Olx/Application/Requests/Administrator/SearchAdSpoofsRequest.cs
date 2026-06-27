namespace Nexus.Olx.Application.Requests.Administrator;

public class SearchAdSpoofsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public string[] OperatorIds { get; set; } = [];
    public string[] OperationIds { get; set; } = [];
}

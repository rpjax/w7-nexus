namespace Nexus.Actors.Requests;

public class SearchOperationsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public string[] AdministratorIds { get; set; } = new string[0];
    public string[] OperatorsIds { get; set; } = new string[0];
    public string[] StrawMansIds { get; set; } = new string[0];
    public string[] GatewayCredentialsIds { get; set; } = new string[0];
}

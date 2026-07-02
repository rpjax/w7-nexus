namespace Nexus.Operations.Application.Requests.OperationAdministrator;

public class SearchTeamLeaderCandidatesRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

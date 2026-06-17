namespace Nexus.TeamLeader.Application.Requests;

public class SearchOperatorsToAssignRequest
{
    public string TeamId { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

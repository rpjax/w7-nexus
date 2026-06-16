namespace Nexus.TeamLeader.Application.Requests;

public class SearchLedTeamsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

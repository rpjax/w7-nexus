namespace Nexus.TeamLeader.Application.Requests;

public class UnassignStrawManFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

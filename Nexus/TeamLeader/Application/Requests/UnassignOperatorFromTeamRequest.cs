namespace Nexus.TeamLeader.Application.Requests;

public class UnassignOperatorFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
}

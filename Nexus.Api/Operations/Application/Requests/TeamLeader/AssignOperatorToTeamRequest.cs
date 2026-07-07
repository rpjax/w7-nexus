namespace Nexus.Operations.Application.Requests.TeamLeader;

public class AssignOperatorToTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
}

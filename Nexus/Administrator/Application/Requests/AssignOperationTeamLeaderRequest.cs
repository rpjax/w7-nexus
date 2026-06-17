namespace Nexus.Administrator.Application.Requests;

public class AssignOperationTeamLeaderRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string TeamLeaderId { get; set; } = string.Empty;
}

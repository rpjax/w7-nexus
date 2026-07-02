namespace Nexus.Operations.Application.Requests.Administrator;

public class AssignOperationTeamLeaderRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string TeamLeaderId { get; set; } = string.Empty;
}

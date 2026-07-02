namespace Nexus.Operations.Application.Requests.Administrator;

public class UnassignOperatorFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
}

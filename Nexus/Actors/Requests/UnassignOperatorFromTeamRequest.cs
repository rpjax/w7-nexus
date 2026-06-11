namespace Nexus.Actors.Requests;

public class UnassignOperatorFromTeamRequest
{
    public string? TeamId { get; set; }
    public string? OperatorId { get; set; }
}

namespace Nexus.Operations.Application.Requests.Administrator;

public class UnassignStrawManFromTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

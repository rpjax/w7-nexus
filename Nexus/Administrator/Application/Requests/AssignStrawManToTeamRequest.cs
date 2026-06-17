namespace Nexus.Administrator.Application.Requests;

public class AssignStrawManToTeamRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string StrawManId { get; set; } = string.Empty;
}

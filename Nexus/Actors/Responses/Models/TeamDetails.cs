using Nexus.Operations.Aggregates;

namespace Nexus.Actors.Responses.Models;

public class TeamDetails
{
    public string Id { get; init; } = default!;
    public string OperationId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? TeamLeaderId { get; init; }

    public static TeamDetails FromTeam(Team team)
    {
        return new TeamDetails
        {
            Id = team.Id,
            OperationId = team.OperationId,
            Name = team.Name,
            TeamLeaderId = team.TeamLeaderId
        };
    }
}

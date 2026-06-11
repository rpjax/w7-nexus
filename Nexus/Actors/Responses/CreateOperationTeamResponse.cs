using Nexus.Actors.Responses.Models;

namespace Nexus.Actors.Responses;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

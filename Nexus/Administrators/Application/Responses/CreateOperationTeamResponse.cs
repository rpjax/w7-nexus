using Nexus.Administrators.Application.Responses.Models;

namespace Nexus.Administrators.Application.Responses;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

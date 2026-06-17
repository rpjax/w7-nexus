using Nexus.Administrator.Application.Responses.Models;

namespace Nexus.Administrator.Application.Responses;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

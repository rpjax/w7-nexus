using Nexus.Operations.Application.Responses.Administrator.Models;

namespace Nexus.Operations.Application.Responses.Administrator;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

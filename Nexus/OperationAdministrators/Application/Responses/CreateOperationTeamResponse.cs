using Nexus.OperationAdministrators.Application.Responses.Models;

namespace Nexus.OperationAdministrators.Application.Responses;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

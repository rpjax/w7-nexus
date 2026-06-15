using Nexus.OperationAdministrator.Application.Responses.Models;

namespace Nexus.OperationAdministrator.Application.Responses;

public class CreateOperationTeamResponse
{
    public TeamDetails Team { get; init; } = default!;
}

using Nexus.OperationAdministrators.Application.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        IReadOnlyList<Operation>? operations = null,
        CancellationToken cancellationToken = default);
}

using Nexus.OperationAdministrator.Application.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(IReadOnlyList<Team> teams, CancellationToken cancellationToken = default);
}

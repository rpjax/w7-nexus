using Nexus.Administrator.Application.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Administrator.Application.Contracts;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(IReadOnlyList<Team> teams, CancellationToken cancellationToken = default);
}

using Nexus.Administrators.Application.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Administrators.Application.Contracts;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(IReadOnlyList<Team> teams, CancellationToken cancellationToken = default);
}

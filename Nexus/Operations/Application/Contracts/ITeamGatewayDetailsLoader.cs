using Nexus.Operations.Application.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamGatewayDetailsLoader
{
    Task<TeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        IReadOnlyList<Operation>? operations = null,
        CancellationToken cancellationToken = default);
}

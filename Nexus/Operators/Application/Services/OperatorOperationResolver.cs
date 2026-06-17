using Aidan.Core.Linq.Extensions;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;

namespace Nexus.Operators.Application.Services;

internal static class OperatorOperationResolver
{
    public static async Task<Team[]> ResolveAssignedTeamsAsync(
        string operatorAccountId,
        ITeamRepository teams)
    {
        var normalizedOperatorAccountId = operatorAccountId.Trim();

        return await teams.AsQueryable()
            .Where(t => t.OperatorIds.Contains(normalizedOperatorAccountId))
            .ToArrayAsync();
    }
}

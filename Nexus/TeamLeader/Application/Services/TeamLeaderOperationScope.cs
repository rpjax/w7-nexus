using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.TeamLeader.Application.Services;

internal static class TeamLeaderOperationScope
{
    public static async Task<IResult<(Team SourceTeam, Team[] OperationTeams)>> ResolveAsync(
        ITeamRepository teams,
        string? teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return Result<(Team, Team[])>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamIdInvalid)
                .WithMessage("O ID da equipe é obrigatório.")
                .Build());
        }

        var normalizedTeamId = teamId.Trim();
        var sourceTeam = await teams.AsQueryable()
            .Where(t => t.Id == normalizedTeamId)
            .FirstOrDefaultAsync();

        if (sourceTeam is null)
        {
            return Result<(Team, Team[])>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamNotFound)
                .WithMessage($"A equipe '{normalizedTeamId}' não foi encontrada.")
                .Build());
        }

        var operationTeams = await teams.AsQueryable()
            .Where(t => t.OperationId == sourceTeam.OperationId)
            .ToArrayAsync();

        return Result<(Team, Team[])>.Success((sourceTeam, operationTeams));
    }

    public static HashSet<string> CollectOperatorIds(IReadOnlyList<Team> teams)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var team in teams)
        {
            foreach (var operatorId in team.OperatorIds)
                ids.Add(operatorId);
        }

        return ids;
    }

    public static HashSet<string> CollectOperationAccountIds(IReadOnlyList<Team> teams)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var team in teams)
        {
            if (!string.IsNullOrWhiteSpace(team.TeamLeaderId))
                ids.Add(team.TeamLeaderId);

            foreach (var operatorId in team.OperatorIds)
                ids.Add(operatorId);

            foreach (var rule in team.OperatorProfitShareRules)
            {
                foreach (var cut in rule.Cuts)
                {
                    if (!string.IsNullOrWhiteSpace(cut.AccountId))
                        ids.Add(cut.AccountId);
                }
            }
        }

        return ids;
    }
}

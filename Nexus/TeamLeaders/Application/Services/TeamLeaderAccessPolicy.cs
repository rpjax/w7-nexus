using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;
using Nexus.TeamLeaders.Application.Contracts;

namespace Nexus.TeamLeaders.Application.Services;

public sealed class TeamLeaderAccessPolicy : ITeamLeaderAccessPolicy
{
    private ITeamRepository _teams { get; }

    public TeamLeaderAccessPolicy(ITeamRepository teams)
    {
        _teams = teams;
    }

    public async Task<IAuthorizationResult> AuthorizeSearchLedTeamsAsync(RequesterIdentity identity)
    {
        var leadsAnyTeam = await _teams.AsQueryable()
            .Where(t => t.TeamLeaderId == identity.AccountId)
            .AnyAsync();

        if (!leadsAnyTeam)
        {
            return AuthorizationResult.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotTeamLeader)
                .WithMessage("Acesso de líder de equipe necessário para realizar esta ação.")
                .Build());
        }

        return AuthorizationResult.Authorized();
    }

    public async Task<IAuthorizationResult> AuthorizeManageTeamAsync(RequesterIdentity identity, string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return AuthorizationResult.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamIdInvalid)
                .WithMessage("O ID da equipe é obrigatório.")
                .Build());
        }

        var normalizedTeamId = teamId.Trim();
        var team = await _teams.AsQueryable()
            .Where(t => t.Id == normalizedTeamId)
            .FirstOrDefaultAsync();

        if (team is null)
        {
            return AuthorizationResult.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamNotFound)
                .WithMessage($"A equipe '{normalizedTeamId}' não foi encontrada.")
                .Build());
        }

        if (team.TeamLeaderId is null ||
            !string.Equals(team.TeamLeaderId, identity.AccountId, StringComparison.Ordinal))
        {
            return AuthorizationResult.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotTeamLeader)
                .WithMessage("Acesso de líder de equipe necessário para realizar esta ação.")
                .Build());
        }

        return AuthorizationResult.Authorized();
    }
}

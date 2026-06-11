using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Actors.Contracts;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Application.Services.Contracts;
using Nexus.Operations.Application.Services.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Authorization.Application.Services;

public sealed class TeamLeaderAccess : ITeamLeaderAccess
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }
    private ITeamRepository _teams { get; }
    private ITeamLeader _teamLeader { get; }

    public TeamLeaderAccess(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts,
        ITeamRepository teams,
        ITeamLeader teamLeader)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
        _teams = teams;
        _teamLeader = teamLeader;
    }

    public async Task<IAccessEvaluationResult<ITeamLeader>> ResolveForTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamIdInvalid)
                .WithMessage("O ID da equipe é obrigatório.")
                .Build());
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("É necessário estar autenticado para realizar esta ação.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta não foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId}' não foi encontrada.")
                .Build());
        }

        var normalizedTeamId = teamId.Trim();
        var team = await _teams.AsQueryable()
            .Where(t => t.Id == normalizedTeamId)
            .FirstOrDefaultAsync();

        if (team is null)
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(TeamErrorCodes.TeamNotFound)
                .WithMessage($"A equipe '{normalizedTeamId}' não foi encontrada.")
                .Build());
        }

        if (team.TeamLeaderId is null ||
            !string.Equals(team.TeamLeaderId, accountId, StringComparison.Ordinal))
        {
            return AccessEvaluationResult<ITeamLeader>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotTeamLeader)
                .WithMessage("Acesso de líder de equipe necessário para realizar esta ação.")
                .Build());
        }

        return AccessEvaluationResult<ITeamLeader>.Authorized(_teamLeader);
    }
}

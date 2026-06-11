using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Actors.Contracts;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Results;
using Nexus.Authorization.Services.Contracts;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Authorization.Services;

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
                .WithMessage("Team ID is required.")
                .Build());
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("An authenticated identity is required.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("Account identity claim is missing.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<ITeamLeader>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"Account '{accountId}' was not found.")
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
                .WithMessage($"Team '{normalizedTeamId}' was not found.")
                .Build());
        }

        if (team.TeamLeaderId is null ||
            !string.Equals(team.TeamLeaderId, accountId, StringComparison.Ordinal))
        {
            return AccessEvaluationResult<ITeamLeader>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotTeamLeader)
                .WithMessage("Team leader access is required.")
                .Build());
        }

        return AccessEvaluationResult<ITeamLeader>.Authorized(_teamLeader);
    }
}

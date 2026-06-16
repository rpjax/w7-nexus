using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Models;
using Nexus.Controllers;
using Nexus.TeamLeader.Application.Contracts;
using Nexus.TeamLeader.Application.Requests;

namespace Nexus.TeamLeader.Presentation;

[Route("api/team-leader")]
[Authorize]
public class TeamLeaderController : NexusController
{
    private ITeamLeaderAccess _teamLeaderAccess { get; }

    public TeamLeaderController(ITeamLeaderAccess teamLeaderAccess)
    {
        _teamLeaderAccess = teamLeaderAccess;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchLedTeamsAsync([FromBody] SearchLedTeamsRequest request)
    {
        var (accessError, teamLeader) = await ResolveForSearchAsync();
        if (accessError is not null)
            return accessError;

        var result = await teamLeader.SearchLedTeamsAsync(request);
        return ToResponse(result);
    }

    [HttpPost("teams/operators")]
    public async Task<ActionResult> AssignOperatorToTeamAsync([FromBody] AssignOperatorToTeamRequest request)
    {
        var (accessError, teamLeader) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await teamLeader.AssignOperatorToTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams/operators")]
    public async Task<ActionResult> UnassignOperatorFromTeamAsync([FromBody] UnassignOperatorFromTeamRequest request)
    {
        var (accessError, teamLeader) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await teamLeader.UnassignOperatorFromTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpPut("teams/operators/profit-share-rules")]
    public async Task<ActionResult> SetOperatorProfitShareRuleAsync(
        [FromBody] SetOperatorProfitShareRuleRequest request)
    {
        var (accessError, teamLeader) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await teamLeader.SetOperatorProfitShareRuleAsync(request!);
        return ToResponse(result);
    }

    private async Task<(ActionResult? Error, ITeamLeader TeamLeader)> ResolveForSearchAsync()
    {
        var access = await _teamLeaderAccess.ResolveAsync();
        return ToAccessResult(access);
    }

    private async Task<(ActionResult? Error, ITeamLeader TeamLeader)> ResolveForTeamAsync(string? teamId)
    {
        var access = await _teamLeaderAccess.ResolveForTeamAsync(teamId ?? string.Empty);
        return ToAccessResult(access);
    }

    private (ActionResult? Error, ITeamLeader TeamLeader) ToAccessResult(IAccessEvaluationResult<ITeamLeader> access)
    {
        if (access.IsFailure)
            return (ProblemResponse(422, access.Errors), default!);

        if (!access.IsAuthorized)
            return (ProblemResponse(403, access.AuthorizationErrors), default!);

        if (access.Role is null)
            throw new InvalidOperationException("Team leader role is missing after successful access evaluation.");

        return (null, access.Role);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.TeamLeader.Application.Contracts;
using Nexus.TeamLeader.Application.Requests;

namespace Nexus.TeamLeader.Presentation;

[Route("api/team-leader")]
[Authorize]
public class TeamLeaderController : NexusController
{
    private ITeamLeader _teamLeader { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public TeamLeaderController(
        ITeamLeader teamLeader,
        IRequesterIdentityResolver identityResolver)
    {
        _teamLeader = teamLeader;
        _identityResolver = identityResolver;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchLedTeamsAsync(
        [FromBody] SearchLedTeamsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _teamLeader.SearchLedTeamsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/operators")]
    public async Task<ActionResult> AssignOperatorToTeamAsync(
        [FromBody] AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _teamLeader.AssignOperatorToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/operators")]
    public async Task<ActionResult> UnassignOperatorFromTeamAsync(
        [FromBody] UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _teamLeader.UnassignOperatorFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("teams/operators/profit-share-rules")]
    public async Task<ActionResult> SetOperatorProfitShareRuleAsync(
        [FromBody] SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _teamLeader.SetOperatorProfitShareRuleAsync(
            identity,
            request,
            cancellationToken));
    }
}

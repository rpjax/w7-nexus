using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Requests;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;

namespace Nexus.Administrators.Presentation;

[Route("api/administrator")]
[Authorize]
public class AdministratorController : NexusController
{
    private IAdministrator _administrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public AdministratorController(
        IAdministrator administrator,
        IRequesterIdentityResolver identityResolver)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
    }

    [HttpPost("operations")]
    public async Task<ActionResult> CreateOperationAsync(
        [FromBody] CreateOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreateOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync(
        [FromBody] SearchOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchOperationsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/search")]
    public async Task<ActionResult> SearchAccountsAsync(
        [FromBody] SearchAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchAccountsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/operators/search")]
    public async Task<ActionResult> SearchOperatorsToAssignAsync(
        [FromBody] SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchOperatorsToAssignAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/profit-share-accounts/search")]
    public async Task<ActionResult> SearchProfitShareAccountsToAssignAsync(
        [FromBody] SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchProfitShareAccountsToAssignAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations")]
    public async Task<ActionResult> DeleteOperationAsync(
        [FromBody] DeleteOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeleteOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/administrators")]
    public async Task<ActionResult> AssignOperationAdministratorAsync(
        [FromBody] AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignOperationAdministratorAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/administrators")]
    public async Task<ActionResult> UnassignOperationAdministratorAsync(
        [FromBody] UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignOperationAdministratorAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams")]
    public async Task<ActionResult> CreateOperationTeamAsync(
        [FromBody] CreateOperationTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreateOperationTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams")]
    public async Task<ActionResult> DeleteOperationTeamAsync(
        [FromBody] DeleteOperationTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeleteOperationTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/leaders")]
    public async Task<ActionResult> AssignOperationTeamLeaderAsync(
        [FromBody] AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignOperationTeamLeaderAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/leaders")]
    public async Task<ActionResult> UnassignOperationTeamLeaderAsync(
        [FromBody] UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignOperationTeamLeaderAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPatch("teams/gateway-selection-strategy")]
    public async Task<ActionResult> SetTeamGatewaySelectionStrategyAsync(
        [FromBody] SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SetTeamGatewaySelectionStrategyAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/straw-men")]
    public async Task<ActionResult> AssignStrawManToTeamAsync(
        [FromBody] AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignStrawManToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/straw-men")]
    public async Task<ActionResult> UnassignStrawManFromTeamAsync(
        [FromBody] UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignStrawManFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/gateway-account-groups")]
    public async Task<ActionResult> AssignGatewayAccountGroupToTeamAsync(
        [FromBody] AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountGroupToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/gateway-account-groups")]
    public async Task<ActionResult> UnassignGatewayAccountGroupFromTeamAsync(
        [FromBody] UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountGroupFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/gateway-accounts")]
    public async Task<ActionResult> AssignGatewayAccountToTeamAsync(
        [FromBody] AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/gateway-accounts")]
    public async Task<ActionResult> UnassignGatewayAccountFromTeamAsync(
        [FromBody] UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountFromTeamAsync(
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

        return ToOperationResult(await _administrator.AssignOperatorToTeamAsync(
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

        return ToOperationResult(await _administrator.UnassignOperatorFromTeamAsync(
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

        return ToOperationResult(await _administrator.SetOperatorProfitShareRuleAsync(
            identity,
            request,
            cancellationToken));
    }
}

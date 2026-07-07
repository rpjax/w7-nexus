using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.OperationAdministrator;

namespace Nexus.Operations.Presentation;

[Route("api/operations/operation-administrator")]
[Authorize]
public class OperationAdministratorController : NexusController
{
    private IOperationAdministrator _operationAdministrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public OperationAdministratorController(
        IOperationAdministrator operationAdministrator,
        IRequesterIdentityResolver identityResolver)
    {
        _operationAdministrator = operationAdministrator;
        _identityResolver = identityResolver;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync(
        [FromBody] SearchOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.SearchOperationsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/team-leader-candidates/search")]
    public async Task<ActionResult> SearchTeamLeaderCandidatesAsync(
        [FromBody] SearchTeamLeaderCandidatesRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.SearchTeamLeaderCandidatesAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/straw-men/search")]
    public async Task<ActionResult> SearchStrawMenToAssignAsync(
        [FromBody] SearchStrawMenToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.SearchStrawMenToAssignAsync(
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

        return ToOperationResult(await _operationAdministrator.CreateOperationTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.DeleteOperationTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.AssignOperationTeamLeaderAsync(
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

        return ToOperationResult(await _operationAdministrator.UnassignOperationTeamLeaderAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("teams/gateway-selection-strategy")]
    public async Task<ActionResult> SetTeamGatewaySelectionStrategyAsync(
        [FromBody] SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.SetTeamGatewaySelectionStrategyAsync(
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

        return ToOperationResult(await _operationAdministrator.AssignStrawManToTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.UnassignStrawManFromTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.AssignGatewayAccountGroupToTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.UnassignGatewayAccountGroupFromTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.AssignGatewayAccountToTeamAsync(
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

        return ToOperationResult(await _operationAdministrator.UnassignGatewayAccountFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("operations/gateway-selection-strategy")]
    public async Task<ActionResult> SetOperationGatewaySelectionStrategyAsync(
        [FromBody] SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.SetOperationGatewaySelectionStrategyAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/straw-men")]
    public async Task<ActionResult> AssignStrawManToOperationAsync(
        [FromBody] AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.AssignStrawManToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/straw-men")]
    public async Task<ActionResult> UnassignStrawManFromOperationAsync(
        [FromBody] UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.UnassignStrawManFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/gateway-account-groups")]
    public async Task<ActionResult> AssignGatewayAccountGroupToOperationAsync(
        [FromBody] AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.AssignGatewayAccountGroupToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/gateway-account-groups")]
    public async Task<ActionResult> UnassignGatewayAccountGroupFromOperationAsync(
        [FromBody] UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.UnassignGatewayAccountGroupFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/gateway-accounts")]
    public async Task<ActionResult> AssignGatewayAccountToOperationAsync(
        [FromBody] AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.AssignGatewayAccountToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/gateway-accounts")]
    public async Task<ActionResult> UnassignGatewayAccountFromOperationAsync(
        [FromBody] UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operationAdministrator.UnassignGatewayAccountFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Models;
using Nexus.Controllers;
using Nexus.OperationAdministrator.Application.Contracts;
using Nexus.OperationAdministrator.Application.Requests;

namespace Nexus.OperationAdministrator.Presentation;

[Route("api/operation-administrator")]
[Authorize]
public class OperationAdministratorController : NexusController
{
    private IOperationAdministratorAccess _operationAdministratorAccess { get; }

    public OperationAdministratorController(IOperationAdministratorAccess operationAdministratorAccess)
    {
        _operationAdministratorAccess = operationAdministratorAccess;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync(
        [FromBody] SearchOperationAdministratorOperationsRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForSearchAsync();
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.SearchOperationsAsync(request);
        return ToResponse(result);
    }

    [HttpPost("teams")]
    public async Task<ActionResult> CreateOperationTeamAsync([FromBody] CreateOperationTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForOperationAsync(request?.OperationId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.CreateOperationTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams")]
    public async Task<ActionResult> DeleteOperationTeamAsync([FromBody] DeleteOperationTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.DeleteOperationTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpPost("teams/leaders")]
    public async Task<ActionResult> AssignOperationTeamLeaderAsync(
        [FromBody] AssignOperationTeamLeaderRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.AssignOperationTeamLeaderAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams/leaders")]
    public async Task<ActionResult> UnassignOperationTeamLeaderAsync(
        [FromBody] UnassignOperationTeamLeaderRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.UnassignOperationTeamLeaderAsync(request!);
        return ToResponse(result);
    }

    [HttpPatch("teams/gateway-selection-strategy")]
    public async Task<ActionResult> SetTeamGatewaySelectionStrategyAsync(
        [FromBody] SetTeamGatewaySelectionStrategyRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.SetTeamGatewaySelectionStrategyAsync(request!);
        return ToResponse(result);
    }

    [HttpPost("teams/straw-men")]
    public async Task<ActionResult> AssignStrawManToTeamAsync([FromBody] AssignStrawManToTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.AssignStrawManToTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams/straw-men")]
    public async Task<ActionResult> UnassignStrawManFromTeamAsync([FromBody] UnassignStrawManFromTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.UnassignStrawManFromTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpPost("teams/gateway-account-groups")]
    public async Task<ActionResult> AssignGatewayAccountGroupToTeamAsync(
        [FromBody] AssignGatewayAccountGroupToTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.AssignGatewayAccountGroupToTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams/gateway-account-groups")]
    public async Task<ActionResult> UnassignGatewayAccountGroupFromTeamAsync(
        [FromBody] UnassignGatewayAccountGroupFromTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.UnassignGatewayAccountGroupFromTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpPost("teams/gateway-accounts")]
    public async Task<ActionResult> AssignGatewayAccountToTeamAsync(
        [FromBody] AssignGatewayAccountToTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.AssignGatewayAccountToTeamAsync(request!);
        return ToResponse(result);
    }

    [HttpDelete("teams/gateway-accounts")]
    public async Task<ActionResult> UnassignGatewayAccountFromTeamAsync(
        [FromBody] UnassignGatewayAccountFromTeamRequest request)
    {
        var (accessError, operationAdministrator) = await ResolveForTeamAsync(request?.TeamId);
        if (accessError is not null)
            return accessError;

        var result = await operationAdministrator.UnassignGatewayAccountFromTeamAsync(request!);
        return ToResponse(result);
    }

    private async Task<(ActionResult? Error, IOperationAdministrator OperationAdministrator)> ResolveForSearchAsync()
    {
        var access = await _operationAdministratorAccess.ResolveAsync();
        return ToAccessResult(access);
    }

    private async Task<(ActionResult? Error, IOperationAdministrator OperationAdministrator)> ResolveForOperationAsync(
        string? operationId)
    {
        var access = await _operationAdministratorAccess.ResolveForOperationAsync(operationId ?? string.Empty);
        return ToAccessResult(access);
    }

    private async Task<(ActionResult? Error, IOperationAdministrator OperationAdministrator)> ResolveForTeamAsync(
        string? teamId)
    {
        var access = await _operationAdministratorAccess.ResolveForTeamAsync(teamId ?? string.Empty);
        return ToAccessResult(access);
    }

    private (ActionResult? Error, IOperationAdministrator OperationAdministrator) ToAccessResult(
        IAccessEvaluationResult<IOperationAdministrator> access)
    {
        if (access.IsFailure)
            return (ProblemResponse(422, access.Errors), default!);

        if (!access.IsAuthorized)
            return (ProblemResponse(403, access.AuthorizationErrors), default!);

        if (access.Role is null)
            throw new InvalidOperationException("Operation administrator role is missing after successful access evaluation.");

        return (null, access.Role);
    }
}

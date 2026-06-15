using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Administrator.Application.Contracts;
using Nexus.Administrator.Application.Requests;
using Nexus.Controllers;

namespace Nexus.Administrator.Presentation;

[Route("api/administrator")]
[Authorize]
public class AdministratorController : NexusController
{
    private IAdministratorAccess _administratorAccess { get; }

    public AdministratorController(IAdministratorAccess administratorAccess)
    {
        _administratorAccess = administratorAccess;
    }

    [HttpPost("operations")]
    public async Task<ActionResult> CreateOperationAsync([FromBody] CreateOperationRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.CreateOperationAsync(request);
        return ToResponse(result);
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync([FromBody] SearchOperationsRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.SearchOperationsAsync(request);
        return ToResponse(result);
    }

    [HttpPost("accounts/search")]
    public async Task<ActionResult> SearchAccountsAsync([FromBody] SearchAccountsRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.SearchAccountsAsync(request);
        return ToResponse(result);
    }

    [HttpDelete("operations")]
    public async Task<ActionResult> DeleteOperationAsync([FromBody] DeleteOperationRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.DeleteOperationAsync(request);
        return ToResponse(result);
    }

    [HttpPost("operations/administrators")]
    public async Task<ActionResult> AssignOperationAdministratorAsync(
        [FromBody] AssignOperationAdministratorRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.AssignOperationAdministratorAsync(request);
        return ToResponse(result);
    }

    [HttpDelete("operations/administrators")]
    public async Task<ActionResult> UnassignOperationAdministratorAsync(
        [FromBody] UnassignOperationAdministratorRequest request)
    {
        var (accessError, administrator) = await ResolveAdministratorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await administrator.UnassignOperationAdministratorAsync(request);
        return ToResponse(result);
    }

    private async Task<(ActionResult? Error, IAdministrator Administrator)> ResolveAdministratorAccessAsync()
    {
        var access = await _administratorAccess.ResolveAsync();
        if (access.IsFailure)
            return (ProblemResponse(422, access.Errors), default!);

        if (!access.IsAuthorized)
            return (ProblemResponse(403, access.AuthorizationErrors), default!);

        if (access.Role is null)
            throw new InvalidOperationException("Administrator role is missing after successful access evaluation.");

        return (null, access.Role);
    }
}

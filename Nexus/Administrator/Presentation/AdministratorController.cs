using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Administrator.Application.Contracts;
using Nexus.Administrator.Application.Requests;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;

namespace Nexus.Administrator.Presentation;

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
}

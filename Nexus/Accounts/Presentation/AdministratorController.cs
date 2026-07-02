using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application.Requests.Administrator;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;

namespace Nexus.Accounts.Presentation;

[Route("api/accounts/administrator")]
[Authorize]
public sealed class AdministratorController : NexusController
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

    [HttpPost("search")]
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

    [HttpPost("roles")]
    public async Task<ActionResult> GrantAccountRoleAsync(
        [FromBody] GrantAccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GrantAccountRoleAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("roles")]
    public async Task<ActionResult> RevokeAccountRoleAsync(
        [FromBody] RevokeAccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RevokeAccountRoleAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("permissions")]
    public async Task<ActionResult> GrantAccountPermissionAsync(
        [FromBody] GrantAccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GrantAccountPermissionAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("permissions")]
    public async Task<ActionResult> RevokeAccountPermissionAsync(
        [FromBody] RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RevokeAccountPermissionAsync(
            identity,
            request,
            cancellationToken));
    }
}

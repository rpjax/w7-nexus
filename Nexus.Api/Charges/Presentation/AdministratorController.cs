using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Models;
using Nexus.Controllers;

namespace Nexus.Charges.Presentation;

[Route("api/charges/administrator")]
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

    [HttpPost("pix")]
    public async Task<ActionResult> CreatePixChargeAsync(
        [FromBody] CreatePixChargeRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreatePixChargeAsync(
            identity,
            request,
            cancellationToken));
    }
}

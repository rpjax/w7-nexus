using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Payments.Application.Models;
using Nexus.StrawMen.Application.Contracts;

namespace Nexus.StrawMen.Presentation;

[Route("api/straw-man")]
[Authorize]
public class StrawManController : NexusController
{
    private IStrawMan _strawMan { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public StrawManController(
        IStrawMan strawMan,
        IRequesterIdentityResolver identityResolver)
    {
        _strawMan = strawMan;
        _identityResolver = identityResolver;
    }

    [HttpPost("payments/search")]
    public async Task<ActionResult> SearchPaymentsAsync(
        [FromBody] SearchPaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _strawMan.SearchPaymentsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpGet("payments/{paymentId}")]
    public async Task<ActionResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _strawMan.GetPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpGet("settings")]
    public async Task<ActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _strawMan.GetSettingsAsync(
            identity,
            cancellationToken));
    }
}

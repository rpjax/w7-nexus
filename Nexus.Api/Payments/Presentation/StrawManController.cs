using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Presentation;

[Route("api/payments/straw-man")]
[Authorize]
public sealed class StrawManController : NexusController
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

    [HttpPost("search")]
    public async Task<ActionResult> SearchPaymentsAsync(
        [FromBody] SearchPaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _strawMan.SearchPaymentsAsync(
            identity,
            request ?? new SearchPaymentsRequest(),
            cancellationToken));
    }

    [HttpGet("{paymentId}")]
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
}

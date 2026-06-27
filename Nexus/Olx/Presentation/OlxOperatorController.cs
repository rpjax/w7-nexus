using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Operator;

namespace Nexus.Olx.Presentation;

[Route("api/olx")]
[Authorize]
public sealed class OlxOperatorController : NexusController
{
    private readonly IOlxOperator _olxOperator;
    private readonly IRequesterIdentityResolver _identityResolver;

    public OlxOperatorController(IOlxOperator olxOperator, IRequesterIdentityResolver identityResolver)
    {
        _olxOperator = olxOperator;
        _identityResolver = identityResolver;
    }

    [HttpPost("ad-spoofs/search")]
    public async Task<ActionResult> SearchAdSpoofsAsync(
        [FromBody] SearchAdSpoofsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxOperator.SearchAdSpoofsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("ads/impersonate")]
    public async Task<ActionResult> ImpersonateAdAsync(
        [FromBody] ImpersonateAdRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxOperator.ImpersonateAdAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("ads/unimpersonate")]
    public async Task<ActionResult> UnimpersonateAdAsync(
        [FromBody] UnimpersonateAdRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxOperator.UnimpersonateAdAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("ads/spoof")]
    public async Task<ActionResult> UpdateAdDetailsSpoofAsync(
        [FromBody] UpdateAdDetailsSpoofRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _olxOperator.UpdateAdDetailsSpoofAsync(
            identity,
            request,
            cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Presentation;

[Route("api/payments/operator")]
[Authorize]
public sealed class OperatorController : NexusController
{
    private IOperator _operator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public OperatorController(IOperator @operator, IRequesterIdentityResolver identityResolver)
    {
        _operator = @operator;
        _identityResolver = identityResolver;
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchPaymentsAsync(
        [FromBody] SearchPaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operator.SearchPaymentsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpGet("{paymentId}")]
    public async Task<ActionResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operator.GetPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }
}

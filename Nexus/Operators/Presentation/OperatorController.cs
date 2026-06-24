using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Requests;
using Nexus.Payments.Application.Models;

namespace Nexus.Operators.Presentation;

[Route("api/operator")]
[Authorize]
public class OperatorController : NexusController
{
    private IOperator _operator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }

    public OperatorController(IOperator @operator, IRequesterIdentityResolver identityResolver)
    {
        _operator = @operator;
        _identityResolver = identityResolver;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync(
        [FromBody] SearchOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _operator.SearchOperationsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("payments/search")]
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

    [HttpGet("payments/{paymentId}")]
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

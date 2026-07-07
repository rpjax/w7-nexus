using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Controllers;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.Operator;

namespace Nexus.Operations.Presentation;

[Route("api/operations/operator")]
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
}

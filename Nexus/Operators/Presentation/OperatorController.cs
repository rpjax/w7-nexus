using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorizations.Application.Contracts;
using Nexus.Controllers;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Requests;

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
}

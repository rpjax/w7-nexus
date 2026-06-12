using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Actors.Contracts;
using Nexus.Actors.Requests;
using Nexus.Authorization.Application.Services.Contracts;

namespace Nexus.Controllers.Operator;

[Route("api/operator")]
[Authorize]
public class OperatorController : NexusController
{
    private IOperatorAccess _operatorAccess { get; }

    public OperatorController(IOperatorAccess operatorAccess)
    {
        _operatorAccess = operatorAccess;
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync([FromBody] SearchOperatorOperationsRequest request)
    {
        var (accessError, @operator) = await ResolveOperatorAccessAsync();
        if (accessError is not null)
            return accessError;

        var result = await @operator.SearchOperationsAsync(request);
        return ToResponse(result);
    }

    private async Task<(ActionResult? Error, IOperator Operator)> ResolveOperatorAccessAsync()
    {
        var access = await _operatorAccess.ResolveAsync();
        if (access.IsFailure)
            return (ProblemResponse(422, access.Errors), default!);

        if (!access.IsAuthorized)
            return (ProblemResponse(403, access.AuthorizationErrors), default!);

        if (access.Role is null)
            throw new InvalidOperationException("Operator role is missing after successful access evaluation.");

        return (null, access.Role);
    }
}

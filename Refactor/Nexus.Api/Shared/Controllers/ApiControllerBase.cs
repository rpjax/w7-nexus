using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Refactor.Nexus.Api.Shared.Controllers;

public abstract class ApiControllerBase : WebController
{
    protected ActionResult ToOperationResult<T>(IOperationResult<T> result)
    {
        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);

        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        if (result.Value is null)
            throw new InvalidOperationException("Operation result value is missing after a successful authorized operation.");

        return Ok(result.Value);
    }
}

using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Nexus.Controllers;

public abstract class NexusController : WebController
{
    protected ActionResult ToResponse<T>(IResult<T> result)
    {
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(result.Value);
    }
}

using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Authorization.Application.Contracts;
using Nexus.Authorization.Application.Models;

namespace Nexus.Controllers;

public abstract class NexusController : WebController
{
    protected static async Task<RequesterIdentity> ResolveIdentityAsync(
        IRequesterIdentityResolver identityResolver,
        CancellationToken cancellationToken)
    {
        var identityResult = await identityResolver.ResolveAsync(cancellationToken);

        if (identityResult.IsFailure || identityResult.Value is not RequesterIdentity identity)
        {
            throw new InvalidOperationException(
                "Requester identity is missing after successful authentication.");
        }

        return identity;
    }

    protected ActionResult ToResponse<T>(IResult<T> result)
    {
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        if (result.Value is null)
            throw new InvalidOperationException("Result value is missing after a successful operation.");

        return Ok(result.Value);
    }

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

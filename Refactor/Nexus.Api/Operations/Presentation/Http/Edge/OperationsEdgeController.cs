using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Operations.Application.UseCases.Edge.Queries;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Operations.Presentation.Http.Edge;

[Route("api/operations/edge")]
[Authorize]
public sealed class OperationsEdgeController : ApiControllerBase
{
    private readonly IResolveScriptUseCase _resolveScript;
    private readonly IGetStoreObjectUseCase _getStoreObject;

    public OperationsEdgeController(
        IResolveScriptUseCase resolveScript,
        IGetStoreObjectUseCase getStoreObject)
    {
        _resolveScript = resolveScript;
        _getStoreObject = getStoreObject;
    }

    [HttpGet("scripts/{operationKey}")]
    public async Task<ActionResult> ResolveScriptAsync(string operationKey, CancellationToken cancellationToken) =>
        ToOperationResult(await _resolveScript.HandleAsync(new ResolveScriptQuery(operationKey), cancellationToken));

    [HttpGet("store/{objectId}")]
    public async Task<ActionResult> GetStoreObjectAsync(string objectId, CancellationToken cancellationToken) =>
        ToOperationResult(await _getStoreObject.HandleAsync(new GetStoreObjectQuery(objectId), cancellationToken));
}

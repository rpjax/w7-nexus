using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Operations.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Operations.Presentation.Http.Administrator;

[Route("api/operations/administrator")]
[Authorize]
public sealed class OperationsAdministratorController : ApiControllerBase
{
    private readonly ICreateOperationUseCase _create;
    private readonly ITransitionOperationUseCase _transition;
    private readonly IConfigureManagementCutUseCase _configureCut;
    private readonly IAssignOperatorUseCase _assign;
    private readonly IUnassignOperatorUseCase _unassign;
    private readonly IRegisterScriptUseCase _registerScript;
    private readonly IUpsertStoreObjectUseCase _upsertStore;
    private readonly IDeleteStoreObjectUseCase _deleteStore;
    private readonly IListOperationsUseCase _list;
    private readonly IGetOperationUseCase _get;
    private readonly IListStoreObjectsUseCase _listStore;

    public OperationsAdministratorController(
        ICreateOperationUseCase create,
        ITransitionOperationUseCase transition,
        IConfigureManagementCutUseCase configureCut,
        IAssignOperatorUseCase assign,
        IUnassignOperatorUseCase unassign,
        IRegisterScriptUseCase registerScript,
        IUpsertStoreObjectUseCase upsertStore,
        IDeleteStoreObjectUseCase deleteStore,
        IListOperationsUseCase list,
        IGetOperationUseCase get,
        IListStoreObjectsUseCase listStore)
    {
        _create = create;
        _transition = transition;
        _configureCut = configureCut;
        _assign = assign;
        _unassign = unassign;
        _registerScript = registerScript;
        _upsertStore = upsertStore;
        _deleteStore = deleteStore;
        _list = list;
        _get = get;
        _listStore = listStore;
    }

    [HttpGet]
    public async Task<ActionResult> ListAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _list.HandleAsync(cancellationToken));

    [HttpGet("{operationId}")]
    public async Task<ActionResult> GetAsync(string operationId, CancellationToken cancellationToken) =>
        ToOperationResult(await _get.HandleAsync(new GetOperationQuery(operationId), cancellationToken));

    [HttpPost]
    public async Task<ActionResult> CreateAsync([FromBody] CreateOperationRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _create.HandleAsync(
            new CreateOperationCommand(request.Name, request.ManagementCutPercent), cancellationToken));

    [HttpPost("{operationId}/transition")]
    public async Task<ActionResult> TransitionAsync(
        string operationId,
        [FromBody] TransitionOperationRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _transition.HandleAsync(
            new TransitionOperationCommand(operationId, request.TargetStatus), cancellationToken));

    [HttpPut("{operationId}/cut")]
    public async Task<ActionResult> ConfigureCutAsync(
        string operationId,
        [FromBody] ConfigureCutRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _configureCut.HandleAsync(
            new ConfigureManagementCutCommand(operationId, request.ManagementCutPercent), cancellationToken));

    [HttpPost("{operationId}/assignments")]
    public async Task<ActionResult> AssignAsync(
        string operationId,
        [FromBody] AssignOperatorRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _assign.HandleAsync(
            new AssignOperatorCommand(operationId, request.MemberId), cancellationToken));

    [HttpDelete("{operationId}/assignments/{memberId}")]
    public async Task<ActionResult> UnassignAsync(string operationId, string memberId, CancellationToken cancellationToken) =>
        ToOperationResult(await _unassign.HandleAsync(
            new UnassignOperatorCommand(operationId, memberId), cancellationToken));

    [HttpPost("{operationId}/scripts")]
    public async Task<ActionResult> RegisterScriptAsync(
        string operationId,
        [FromBody] RegisterScriptRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _registerScript.HandleAsync(
            new RegisterScriptCommand(operationId, request.Name, request.Body), cancellationToken));

    [HttpGet("{operationId}/store")]
    public async Task<ActionResult> ListStoreAsync(
        string operationId,
        [FromQuery] string? objectType,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _listStore.HandleAsync(
            new ListStoreObjectsQuery(operationId, objectType), cancellationToken));

    [HttpPut("{operationId}/store")]
    public async Task<ActionResult> UpsertStoreAsync(
        string operationId,
        [FromBody] UpsertStoreObjectRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _upsertStore.HandleAsync(
            new UpsertStoreObjectCommand(operationId, request.ObjectId, request.ObjectType, request.PayloadJson),
            cancellationToken));

    [HttpDelete("{operationId}/store/{objectId}")]
    public async Task<ActionResult> DeleteStoreAsync(string operationId, string objectId, CancellationToken cancellationToken) =>
        ToOperationResult(await _deleteStore.HandleAsync(
            new DeleteStoreObjectCommand(operationId, objectId), cancellationToken));
}

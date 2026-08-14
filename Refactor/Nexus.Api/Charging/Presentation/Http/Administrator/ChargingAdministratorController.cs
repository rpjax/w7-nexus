using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Charging.Application.UseCases.Authenticated.Commands;
using Refactor.Nexus.Api.Charging.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Charging.Presentation.Http.Administrator;

[Route("api/charging/administrator")]
[Authorize]
public sealed class ChargingAdministratorController : ApiControllerBase
{
    private readonly IBindEmissionRailUseCase _bind;
    private readonly IUnbindEmissionRailUseCase _unbind;
    private readonly IListEmissionRailsUseCase _listRails;
    private readonly IListOperationEmissionSetUseCase _listSet;
    private readonly IListChargesUseCase _listCharges;
    private readonly IGetChargeUseCase _getCharge;
    private readonly ITransitionChargeUseCase _transition;
    private readonly IMarkChargePaidUseCase _markPaid;
    private readonly IHostEnvironment _environment;

    public ChargingAdministratorController(
        IBindEmissionRailUseCase bind,
        IUnbindEmissionRailUseCase unbind,
        IListEmissionRailsUseCase listRails,
        IListOperationEmissionSetUseCase listSet,
        IListChargesUseCase listCharges,
        IGetChargeUseCase getCharge,
        ITransitionChargeUseCase transition,
        IMarkChargePaidUseCase markPaid,
        IHostEnvironment environment)
    {
        _bind = bind;
        _unbind = unbind;
        _listRails = listRails;
        _listSet = listSet;
        _listCharges = listCharges;
        _getCharge = getCharge;
        _transition = transition;
        _markPaid = markPaid;
        _environment = environment;
    }

    [HttpGet("rails")]
    public async Task<ActionResult> ListRailsAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _listRails.HandleAsync(cancellationToken));

    [HttpGet("operations/{operationId}/rails")]
    public async Task<ActionResult> ListSetAsync(string operationId, CancellationToken cancellationToken) =>
        ToOperationResult(await _listSet.HandleAsync(new ListOperationEmissionSetQuery(operationId), cancellationToken));

    [HttpPost("operations/{operationId}/rails/{railId}")]
    public async Task<ActionResult> BindAsync(string operationId, string railId, CancellationToken cancellationToken) =>
        ToOperationResult(await _bind.HandleAsync(new BindEmissionRailCommand(operationId, railId), cancellationToken));

    [HttpDelete("operations/{operationId}/rails/{railId}")]
    public async Task<ActionResult> UnbindAsync(string operationId, string railId, CancellationToken cancellationToken) =>
        ToOperationResult(await _unbind.HandleAsync(new UnbindEmissionRailCommand(operationId, railId), cancellationToken));

    [HttpGet]
    public async Task<ActionResult> ListChargesAsync([FromQuery] string? operationId, [FromQuery] string? operatorMemberId, CancellationToken cancellationToken) =>
        ToOperationResult(await _listCharges.HandleAsync(new ListChargesQuery(operationId, operatorMemberId), cancellationToken));

    [HttpGet("{chargeId}")]
    public async Task<ActionResult> GetChargeAsync(string chargeId, CancellationToken cancellationToken) =>
        ToOperationResult(await _getCharge.HandleAsync(new GetChargeQuery(chargeId), cancellationToken));

    [HttpPost("{chargeId}/transition")]
    public async Task<ActionResult> TransitionAsync(string chargeId, [FromBody] TransitionChargeRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _transition.HandleAsync(new TransitionChargeCommand(chargeId, request.Target), cancellationToken));

    [HttpPost("{chargeId}/mark-paid")]
    public async Task<ActionResult> MarkPaidDevAsync(string chargeId, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        return ToOperationResult(await _markPaid.HandleAsync(new MarkChargePaidCommand(chargeId, null), cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Charging.Application.UseCases.Authenticated.Commands;
using Refactor.Nexus.Api.Charging.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Charging.Presentation.Http.Authenticated;

[Route("api/charging/authenticated")]
[Authorize]
public sealed class ChargingAuthenticatedController : ApiControllerBase
{
    private readonly ICreateChargeUseCase _create;
    private readonly IListChargesUseCase _list;
    private readonly IGetChargeUseCase _get;

    public ChargingAuthenticatedController(
        ICreateChargeUseCase create,
        IListChargesUseCase list,
        IGetChargeUseCase get)
    {
        _create = create;
        _list = list;
        _get = get;
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync([FromBody] CreateChargeRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _create.HandleAsync(
            new CreateChargeCommand(request.OperationId, request.GrossAmount, request.Currency, request.EmissionRailId, request.OperatorMemberId),
            cancellationToken));

    [HttpGet]
    public async Task<ActionResult> ListMineAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _list.HandleAsync(new ListChargesQuery(null, null), cancellationToken));

    [HttpGet("{chargeId}")]
    public async Task<ActionResult> GetAsync(string chargeId, CancellationToken cancellationToken) =>
        ToOperationResult(await _get.HandleAsync(new GetChargeQuery(chargeId), cancellationToken));
}

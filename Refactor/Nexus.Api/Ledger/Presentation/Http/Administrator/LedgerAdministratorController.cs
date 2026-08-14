using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Ledger.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Ledger.Presentation.Http.Administrator;

[Route("api/ledger/administrator")]
[Authorize]
public sealed class LedgerAdministratorController : ApiControllerBase
{
    private readonly IMaterializeChargeUseCase _materialize;
    private readonly IRegisterHopUseCase _registerHop;
    private readonly IRepassClaimsUseCase _repass;
    private readonly IListClaimsUseCase _list;
    private readonly IGetClaimUseCase _get;
    private readonly IListHopsUseCase _listHops;
    private readonly IRevealClaimUseCase _reveal;
    private readonly IMarkAccountLostUseCase _markLost;
    private readonly IReconcileAccountUseCase _reconcile;
    private readonly IReverseChargeUseCase _reverse;
    private readonly IListExposureUseCase _exposure;
    private readonly IArchiveClaimUseCase _archive;

    public LedgerAdministratorController(
        IMaterializeChargeUseCase materialize,
        IRegisterHopUseCase registerHop,
        IRepassClaimsUseCase repass,
        IListClaimsUseCase list,
        IGetClaimUseCase get,
        IListHopsUseCase listHops,
        IRevealClaimUseCase reveal,
        IMarkAccountLostUseCase markLost,
        IReconcileAccountUseCase reconcile,
        IReverseChargeUseCase reverse,
        IListExposureUseCase exposure,
        IArchiveClaimUseCase archive)
    {
        _materialize = materialize;
        _registerHop = registerHop;
        _repass = repass;
        _list = list;
        _get = get;
        _listHops = listHops;
        _reveal = reveal;
        _markLost = markLost;
        _reconcile = reconcile;
        _reverse = reverse;
        _exposure = exposure;
        _archive = archive;
    }

    [HttpPost("materializations")]
    public async Task<ActionResult> MaterializeAsync(
        [FromBody] MaterializeChargeRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _materialize.HandleAsync(
            new MaterializeChargeCommand(
                request.ChargeId,
                request.NetAmount,
                request.Currency,
                request.LandingWorldAccountId),
            cancellationToken));

    [HttpGet("claims")]
    public async Task<ActionResult> ListAsync(
        [FromQuery] string? chargeId,
        [FromQuery] string? accountId,
        [FromQuery] string? beneficiaryId,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _list.HandleAsync(new ListClaimsQuery(chargeId, accountId, beneficiaryId), cancellationToken));

    [HttpGet("claims/{claimId}")]
    public async Task<ActionResult> GetAsync(string claimId, CancellationToken cancellationToken) =>
        ToOperationResult(await _get.HandleAsync(new GetClaimQuery(claimId), cancellationToken));

    [HttpPost("hops")]
    public async Task<ActionResult> RegisterHopAsync(
        [FromBody] RegisterHopRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _registerHop.HandleAsync(
            new RegisterHopCommand(
                request.OriginAccountId,
                request.Currency,
                request.ClaimIds,
                (request.Destinations ?? []).Select(d => new HopDestinationInput(d.AccountId, d.Amount, d.Currency)).ToList(),
                request.Cut is null
                    ? null
                    : new HopCutInput(
                        request.Cut.OrangeMemberId,
                        request.Cut.Percent,
                        request.Cut.InPlace,
                        request.Cut.OrangeAccountId),
                request.KeepRemainderAtOrigin,
                request.LossCause),
            cancellationToken));

    [HttpPost("repasse")]
    public async Task<ActionResult> RepassAsync(
        [FromBody] RepassClaimsRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _repass.HandleAsync(
            new RepassClaimsCommand(request.OriginAccountId, request.ClaimIds, request.PayoutAccountId),
            cancellationToken));

    [HttpGet("hops")]
    public async Task<ActionResult> ListHopsAsync(
        [FromQuery] string? accountId,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _listHops.HandleAsync(new ListHopsQuery(accountId), cancellationToken));

    [HttpPost("claims/{claimId}/reveal")]
    public async Task<ActionResult> RevealAsync(
        string claimId,
        [FromBody] RevealClaimRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _reveal.HandleAsync(
            new RevealClaimCommand(claimId, request.Summary),
            cancellationToken));

    [HttpPost("accounts/{accountId}/lost")]
    public async Task<ActionResult> MarkLostAsync(
        string accountId,
        [FromBody] MarkAccountLostRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _markLost.HandleAsync(
            new MarkAccountLostCommand(accountId, request.Cause),
            cancellationToken));

    [HttpPost("accounts/{accountId}/reconcile")]
    public async Task<ActionResult> ReconcileAsync(
        string accountId,
        [FromBody] ReconcileAccountRequest request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _reconcile.HandleAsync(
            new ReconcileAccountCommand(
                accountId,
                request.Currency,
                request.ObservedBalance,
                request.Cause,
                request.ClaimId),
            cancellationToken));

    [HttpPost("charges/{chargeId}/reverse")]
    public async Task<ActionResult> ReverseAsync(
        string chargeId,
        [FromBody] ReverseChargeRequest? request,
        CancellationToken cancellationToken) =>
        ToOperationResult(await _reverse.HandleAsync(
            new ReverseChargeCommand(chargeId, request?.Cause ?? ""),
            cancellationToken));

    [HttpPost("claims/{claimId}/archive")]
    public async Task<ActionResult> ArchiveAsync(string claimId, CancellationToken cancellationToken) =>
        ToOperationResult(await _archive.HandleAsync(new ArchiveClaimCommand(claimId), cancellationToken));

    [HttpGet("exposure")]
    public async Task<ActionResult> ExposureAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _exposure.HandleAsync(new ListExposureQuery(), cancellationToken));
}

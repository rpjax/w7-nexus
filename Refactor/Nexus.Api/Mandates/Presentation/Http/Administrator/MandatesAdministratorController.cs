using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.CloseAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantCapability;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantPreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RemoveShareholderStake;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokeCapability;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokePreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertShareholderStake;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.GetMemberMandate;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListAgencyDeals;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListShareholders;
using Refactor.Nexus.Api.Mandates.Presentation.Http.Contracts;
using Refactor.Nexus.Api.Shared.Controllers;

namespace Refactor.Nexus.Api.Mandates.Presentation.Http.Administrator;

[Route("api/mandates/administrator")]
[Authorize]
public sealed class MandatesAdministratorController : ApiControllerBase
{
    private readonly IGrantPresetUseCase _grantPreset;
    private readonly IRevokePresetUseCase _revokePreset;
    private readonly IGrantCapabilityUseCase _grantCapability;
    private readonly IRevokeCapabilityUseCase _revokeCapability;
    private readonly IUpsertAgencyDealUseCase _upsertAgencyDeal;
    private readonly ICloseAgencyDealUseCase _closeAgencyDeal;
    private readonly IUpsertShareholderStakeUseCase _upsertShareholderStake;
    private readonly IRemoveShareholderStakeUseCase _removeShareholderStake;
    private readonly IGetMemberMandateUseCase _getMemberMandate;
    private readonly IListAgencyDealsUseCase _listAgencyDeals;
    private readonly IListShareholdersUseCase _listShareholders;

    public MandatesAdministratorController(
        IGrantPresetUseCase grantPreset,
        IRevokePresetUseCase revokePreset,
        IGrantCapabilityUseCase grantCapability,
        IRevokeCapabilityUseCase revokeCapability,
        IUpsertAgencyDealUseCase upsertAgencyDeal,
        ICloseAgencyDealUseCase closeAgencyDeal,
        IUpsertShareholderStakeUseCase upsertShareholderStake,
        IRemoveShareholderStakeUseCase removeShareholderStake,
        IGetMemberMandateUseCase getMemberMandate,
        IListAgencyDealsUseCase listAgencyDeals,
        IListShareholdersUseCase listShareholders)
    {
        _grantPreset = grantPreset;
        _revokePreset = revokePreset;
        _grantCapability = grantCapability;
        _revokeCapability = revokeCapability;
        _upsertAgencyDeal = upsertAgencyDeal;
        _closeAgencyDeal = closeAgencyDeal;
        _upsertShareholderStake = upsertShareholderStake;
        _removeShareholderStake = removeShareholderStake;
        _getMemberMandate = getMemberMandate;
        _listAgencyDeals = listAgencyDeals;
        _listShareholders = listShareholders;
    }

    [HttpGet("members/{accountId}")]
    public async Task<ActionResult> GetMemberMandateAsync(string accountId, CancellationToken cancellationToken) =>
        ToOperationResult(await _getMemberMandate.HandleAsync(new GetMemberMandateQuery(accountId), cancellationToken));

    [HttpPost("presets")]
    public async Task<ActionResult> GrantPresetAsync([FromBody] PresetRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _grantPreset.HandleAsync(new GrantPresetCommand(request.AccountId, request.PresetId), cancellationToken));

    [HttpDelete("presets")]
    public async Task<ActionResult> RevokePresetAsync([FromBody] PresetRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _revokePreset.HandleAsync(new RevokePresetCommand(request.AccountId, request.PresetId), cancellationToken));

    [HttpPost("capabilities")]
    public async Task<ActionResult> GrantCapabilityAsync([FromBody] CapabilityRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _grantCapability.HandleAsync(
            new GrantCapabilityCommand(request.AccountId, request.Capability, request.ScopeKind, request.OperationIds), cancellationToken));

    [HttpDelete("capabilities")]
    public async Task<ActionResult> RevokeCapabilityAsync([FromBody] CapabilityRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _revokeCapability.HandleAsync(
            new RevokeCapabilityCommand(request.AccountId, request.Capability, request.ScopeKind, request.OperationIds), cancellationToken));

    [HttpGet("deals")]
    public async Task<ActionResult> ListDealsAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _listAgencyDeals.HandleAsync(cancellationToken));

    [HttpPut("deals")]
    public async Task<ActionResult> UpsertDealAsync([FromBody] UpsertDealRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _upsertAgencyDeal.HandleAsync(
            new UpsertAgencyDealCommand(
                request.RecruiterAccountId,
                request.OperatorAccountId,
                request.OperatorPercent,
                request.RecruiterPercent),
            cancellationToken));

    [HttpPost("deals/close")]
    public async Task<ActionResult> CloseDealAsync([FromBody] CloseDealRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _closeAgencyDeal.HandleAsync(
            new CloseAgencyDealCommand(request.OperatorAccountId), cancellationToken));

    [HttpGet("shareholders")]
    public async Task<ActionResult> ListShareholdersAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _listShareholders.HandleAsync(cancellationToken));

    [HttpPut("shareholders")]
    public async Task<ActionResult> UpsertShareholderAsync([FromBody] UpsertShareholderRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _upsertShareholderStake.HandleAsync(
            new UpsertShareholderStakeCommand(request.AccountId, request.Percentage), cancellationToken));

    [HttpDelete("shareholders/{accountId}")]
    public async Task<ActionResult> RemoveShareholderAsync(string accountId, CancellationToken cancellationToken) =>
        ToOperationResult(await _removeShareholderStake.HandleAsync(
            new RemoveShareholderStakeCommand(accountId), cancellationToken));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Shared.Controllers;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.WorldAccounts.Presentation.Http.Contracts;

namespace Refactor.Nexus.Api.WorldAccounts.Presentation.Http.Administrator;

[Route("api/world-accounts/administrator")]
[Authorize]
public sealed class WorldAccountsAdministratorController : ApiControllerBase
{
    private readonly IOpenWorldAccountUseCase _open;
    private readonly ILabelWorldAccountUseCase _label;
    private readonly IConfigureWorldAccountUseCase _configure;
    private readonly IRecordWorldAccountObservationUseCase _observe;
    private readonly IListWorldAccountsUseCase _list;
    private readonly IGetWorldAccountUseCase _get;
    private readonly IListWorldAccountTransactionsUseCase _transactions;

    public WorldAccountsAdministratorController(
        IOpenWorldAccountUseCase open,
        ILabelWorldAccountUseCase label,
        IConfigureWorldAccountUseCase configure,
        IRecordWorldAccountObservationUseCase observe,
        IListWorldAccountsUseCase list,
        IGetWorldAccountUseCase get,
        IListWorldAccountTransactionsUseCase transactions)
    {
        _open = open;
        _label = label;
        _configure = configure;
        _observe = observe;
        _list = list;
        _get = get;
        _transactions = transactions;
    }

    [HttpGet]
    public async Task<ActionResult> ListAsync(CancellationToken cancellationToken) =>
        ToOperationResult(await _list.HandleAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult> OpenAsync([FromBody] OpenWorldAccountRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _open.HandleAsync(
            new OpenWorldAccountCommand(
                request.Kind,
                request.Label,
                request.OrangeMemberId,
                request.Level1CutPercent,
                request.QuotaCurrency,
                request.QuotaRemaining),
            cancellationToken));

    [HttpGet("{accountId}")]
    public async Task<ActionResult> GetAsync(string accountId, CancellationToken cancellationToken) =>
        ToOperationResult(await _get.HandleAsync(new GetWorldAccountQuery(accountId), cancellationToken));

    [HttpPut("{accountId}/label")]
    public async Task<ActionResult> LabelAsync(string accountId, [FromBody] LabelWorldAccountRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _label.HandleAsync(new LabelWorldAccountCommand(accountId, request.Label), cancellationToken));

    [HttpPut("{accountId}")]
    public async Task<ActionResult> ConfigureAsync(string accountId, [FromBody] ConfigureWorldAccountRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _configure.HandleAsync(
            new ConfigureWorldAccountCommand(
                accountId,
                request.Level1CutPercent,
                request.OrangeMemberId,
                request.QuotaCurrency,
                request.QuotaRemaining,
                request.EmissionStatus,
                request.BalanceStatus),
            cancellationToken));

    [HttpPost("{accountId}/observations")]
    public async Task<ActionResult> ObserveAsync(string accountId, [FromBody] RecordObservationRequest request, CancellationToken cancellationToken) =>
        ToOperationResult(await _observe.HandleAsync(
            new RecordWorldAccountObservationCommand(
                accountId,
                request.Direction,
                request.Currency,
                request.Amount,
                request.Memo),
            cancellationToken));

    [HttpGet("{accountId}/transactions")]
    public async Task<ActionResult> TransactionsAsync(string accountId, CancellationToken cancellationToken) =>
        ToOperationResult(await _transactions.HandleAsync(new ListWorldAccountTransactionsQuery(accountId), cancellationToken));
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.WorldAccounts.Application.Journal;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Ledger;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Shared;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using Refactor.Nexus.Api.WorldAccounts.Domain.Errors;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Commands;

public sealed record OpenWorldAccountCommand(
    string Kind,
    string Label,
    string? OrangeMemberId,
    decimal? Level1CutPercent,
    string? QuotaCurrency,
    decimal? QuotaRemaining);

public sealed record OpenWorldAccountResult(Guid AccountId);

public interface IOpenWorldAccountUseCase
{
    Task<IOperationResult<OpenWorldAccountResult>> HandleAsync(
        OpenWorldAccountCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class OpenWorldAccountHandler : IOpenWorldAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;
    private readonly IJournalWriter _journal;

    public OpenWorldAccountHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
        _journal = journal;
    }

    public async Task<IOperationResult<OpenWorldAccountResult>> HandleAsync(
        OpenWorldAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<OpenWorldAccountResult>.Failure(WorldAccountGuards.BodyRequired());

        var auth = await WorldAccountGuards.AuthorizeManageAsync<OpenWorldAccountResult>(
            _requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Enum.TryParse<WorldAccountKind>(command.Kind, true, out var kind))
        {
            return OperationResult<OpenWorldAccountResult>.Failure(Error.Create()
                .WithCode(WorldAccountErrorCodes.KindInvalid)
                .WithMessage("Tipo de Conta invalido.")
                .Build());
        }

        Guid? orangeId = null;
        if (!string.IsNullOrWhiteSpace(command.OrangeMemberId))
        {
            if (!Guid.TryParse(command.OrangeMemberId, out var parsed))
            {
                return OperationResult<OpenWorldAccountResult>.Failure(Error.Create()
                    .WithCode(WorldAccountErrorCodes.OrangeNotEligible)
                    .WithMessage("Laranja inválido.")
                    .Build());
            }

            orangeId = parsed;
        }

        if (kind == WorldAccountKind.Gateway)
        {
            if (orangeId is null || !await _access.IsEligibleOrangeAsync(orangeId.Value, cancellationToken))
            {
                return OperationResult<OpenWorldAccountResult>.Failure(Error.Create()
                    .WithCode(WorldAccountErrorCodes.OrangeNotEligible)
                    .WithMessage("Escolha um membro que atua como Laranja. Um login comum não abre Gateway.")
                    .Build());
            }
        }

        var opened = WorldAccountAggregate.Open(
            kind,
            command.Label,
            orangeId,
            command.Level1CutPercent,
            command.QuotaCurrency,
            command.QuotaRemaining);
        if (opened.IsFailure)
            return OperationResult<OpenWorldAccountResult>.Failure(opened.Errors);

        await _repository.SaveAsync(opened.Value!, cancellationToken);
        _journal.RecordOpened(opened.Value!.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<OpenWorldAccountResult>.Success(new OpenWorldAccountResult(opened.Value!.Id));
    }
}

public sealed record LabelWorldAccountCommand(string AccountId, string Label);
public sealed class LabelWorldAccountResult;
public interface ILabelWorldAccountUseCase
{
    Task<IOperationResult<LabelWorldAccountResult>> HandleAsync(
        LabelWorldAccountCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class LabelWorldAccountHandler : ILabelWorldAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;
    private readonly IJournalWriter _journal;

    public LabelWorldAccountHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
        _journal = journal;
    }

    public async Task<IOperationResult<LabelWorldAccountResult>> HandleAsync(
        LabelWorldAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var loaded = await WorldAccountCommandSupport.LoadManagedAsync<LabelWorldAccountResult>(
            _requestContext, _access, _repository, command.AccountId, cancellationToken);
        if (loaded.Failure is not null)
            return loaded.Failure;

        var result = loaded.Account!.Relabel(command.Label);
        if (result.IsFailure)
            return OperationResult<LabelWorldAccountResult>.Failure(result.Errors);

        await _repository.SaveAsync(loaded.Account, cancellationToken);
        _journal.RecordLabeled(loaded.Account.Id, Guid.Parse(loaded.Requester!.AccountId));
        return OperationResult<LabelWorldAccountResult>.Success(new LabelWorldAccountResult());
    }
}

public sealed record ConfigureWorldAccountCommand(
    string AccountId,
    decimal? Level1CutPercent,
    string? OrangeMemberId,
    string? QuotaCurrency,
    decimal? QuotaRemaining,
    string? EmissionStatus,
    string? BalanceStatus);

public sealed class ConfigureWorldAccountResult;
public interface IConfigureWorldAccountUseCase
{
    Task<IOperationResult<ConfigureWorldAccountResult>> HandleAsync(
        ConfigureWorldAccountCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ConfigureWorldAccountHandler : IConfigureWorldAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;
    private readonly IJournalWriter _journal;

    public ConfigureWorldAccountHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
        _journal = journal;
    }

    public async Task<IOperationResult<ConfigureWorldAccountResult>> HandleAsync(
        ConfigureWorldAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var loaded = await WorldAccountCommandSupport.LoadManagedAsync<ConfigureWorldAccountResult>(
            _requestContext, _access, _repository, command.AccountId, cancellationToken);
        if (loaded.Failure is not null)
            return loaded.Failure;

        var account = loaded.Account!;
        var changingEmission = !string.IsNullOrWhiteSpace(command.EmissionStatus);
        var changingBalance = !string.IsNullOrWhiteSpace(command.BalanceStatus);
        if (account.BalanceStatus == BalanceStatus.Lost && (changingEmission || changingBalance))
        {
            return OperationResult<ConfigureWorldAccountResult>.Failure(Error.Create()
                .WithCode(WorldAccountErrorCodes.BalanceLost)
                .WithMessage("Esta conta já está perdida; emissão e saldo não mudam daqui.")
                .Build());
        }

        Guid? orange = null;
        if (!string.IsNullOrWhiteSpace(command.OrangeMemberId))
        {
            if (!Guid.TryParse(command.OrangeMemberId, out var parsed)
                || !await _access.IsEligibleOrangeAsync(parsed, cancellationToken))
            {
                return OperationResult<ConfigureWorldAccountResult>.Failure(Error.Create()
                    .WithCode(WorldAccountErrorCodes.OrangeNotEligible)
                    .WithMessage("Escolha um membro que atua como Laranja. Um login comum não abre Gateway.")
                    .Build());
            }

            orange = parsed;
        }

        if (command.Level1CutPercent is not null || orange is not null)
        {
            var configured = account.ConfigureGateway(command.Level1CutPercent, orange);
            if (configured.IsFailure)
                return OperationResult<ConfigureWorldAccountResult>.Failure(configured.Errors);
        }

        if (command.QuotaRemaining is not null)
        {
            var quota = account.ConfigureQuota(command.QuotaCurrency ?? "BRL", command.QuotaRemaining.Value);
            if (quota.IsFailure)
                return OperationResult<ConfigureWorldAccountResult>.Failure(quota.Errors);
        }

        if (!string.IsNullOrWhiteSpace(command.EmissionStatus)
            && Enum.TryParse<EmissionStatus>(command.EmissionStatus, true, out var emission))
        {
            var status = account.SetEmissionStatus(emission);
            if (status.IsFailure)
                return OperationResult<ConfigureWorldAccountResult>.Failure(status.Errors);
        }

        if (!string.IsNullOrWhiteSpace(command.BalanceStatus)
            && Enum.TryParse<BalanceStatus>(command.BalanceStatus, true, out var balance))
        {
            if (balance == BalanceStatus.Lost)
            {
                return OperationResult<ConfigureWorldAccountResult>.Failure(Error.Create()
                    .WithCode(WorldAccountErrorCodes.UseLostEndpoint)
                    .WithMessage("Saldo perdido dispara write-off; use POST /api/ledger/administrator/accounts/{id}/lost.")
                    .Build());
            }

            var status = account.SetBalanceStatus(balance);
            if (status.IsFailure)
                return OperationResult<ConfigureWorldAccountResult>.Failure(status.Errors);
        }

        await _repository.SaveAsync(account, cancellationToken);
        _journal.RecordConfigured(account.Id, Guid.Parse(loaded.Requester!.AccountId));
        return OperationResult<ConfigureWorldAccountResult>.Success(new ConfigureWorldAccountResult());
    }
}

public sealed record RecordWorldAccountObservationCommand(
    string AccountId,
    string Direction,
    string Currency,
    decimal Amount,
    string? Memo);

public sealed class RecordWorldAccountObservationResult;
public interface IRecordWorldAccountObservationUseCase
{
    Task<IOperationResult<RecordWorldAccountObservationResult>> HandleAsync(
        RecordWorldAccountObservationCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RecordWorldAccountObservationHandler : IRecordWorldAccountObservationUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IWorldAccountAccess _access;
    private readonly IWorldAccountRepository _repository;
    private readonly ILedgerClaimObservationPort _ledgerClaims;
    private readonly IJournalWriter _journal;

    public RecordWorldAccountObservationHandler(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository,
        ILedgerClaimObservationPort ledgerClaims,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _repository = repository;
        _ledgerClaims = ledgerClaims;
        _journal = journal;
    }

    public async Task<IOperationResult<RecordWorldAccountObservationResult>> HandleAsync(
        RecordWorldAccountObservationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RecordWorldAccountObservationResult>.Failure(WorldAccountGuards.BodyRequired());

        var loaded = await WorldAccountCommandSupport.LoadManagedAsync<RecordWorldAccountObservationResult>(
            _requestContext, _access, _repository, command.AccountId, cancellationToken);
        if (loaded.Failure is not null)
            return loaded.Failure;

        if (loaded.Account!.BalanceStatus == BalanceStatus.Lost)
        {
            return OperationResult<RecordWorldAccountObservationResult>.Failure(Error.Create()
                .WithCode(WorldAccountErrorCodes.BalanceLost)
                .WithMessage("Esta conta já está perdida; crédito e débito não se aplicam.")
                .Build());
        }

        var currency = (command.Currency ?? "").Trim().ToUpperInvariant();
        var presence = await _ledgerClaims.GetPresenceAsync(loaded.Account!.Id, currency, cancellationToken);
        if (presence.HasAny)
        {
            var message = presence.HasActive
                ? "Ha claims ativos nesta Conta/moeda; movimento de caixa e reconciliacao, nao observacao."
                : "Observacao so e seed; o ledger ja tocou esta Conta/moeda — use reconciliacao.";
            return OperationResult<RecordWorldAccountObservationResult>.Failure(Error.Create()
                .WithCode(WorldAccountErrorCodes.ObservationSeedOnly)
                .WithMessage(message)
                .Build());
        }

        var direction = command.Direction.Trim();
        IResult mutation = direction.Equals("debit", StringComparison.OrdinalIgnoreCase)
            ? loaded.Account!.Debit(command.Currency, command.Amount, command.Memo)
            : direction.Equals("credit", StringComparison.OrdinalIgnoreCase)
                ? loaded.Account!.Credit(command.Currency, command.Amount, command.Memo)
                : Result.Failure(Error.Create()
                    .WithCode(WorldAccountErrorCodes.InvalidAmount)
                    .WithMessage("Direcao deve ser credit ou debit.")
                    .Build());

        if (mutation.IsFailure)
            return OperationResult<RecordWorldAccountObservationResult>.Failure(mutation.Errors);

        await _repository.SaveAsync(loaded.Account!, cancellationToken);
        _journal.RecordObservation(loaded.Account!.Id, Guid.Parse(loaded.Requester!.AccountId));
        return OperationResult<RecordWorldAccountObservationResult>.Success(new RecordWorldAccountObservationResult());
    }
}

internal static class WorldAccountCommandSupport
{
    public static async Task<(WorldAccountAggregate? Account, RequesterContext? Requester, IOperationResult<T>? Failure)> LoadManagedAsync<T>(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        IWorldAccountRepository repository,
        string accountId,
        CancellationToken cancellationToken)
    {
        var auth = await WorldAccountGuards.AuthorizeManageAsync<T>(requestContext, access, cancellationToken);
        if (auth.Failure is not null)
            return (null, null, auth.Failure);

        if (!Guid.TryParse(accountId, out var id))
            return (null, null, OperationResult<T>.Failure(WorldAccountGuards.NotFound(accountId)));

        var account = await repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
            return (null, null, OperationResult<T>.Failure(WorldAccountGuards.NotFound(accountId)));

        return (account, auth.Requester, null);
    }
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record ReconcileAccountCommand(
    string AccountId,
    string Currency,
    decimal ObservedBalance,
    string Cause,
    string? ClaimId);

public sealed record ReconcileAccountResult(Guid AccountId, decimal NexusBalance, decimal ObservedBalance);

public interface IReconcileAccountUseCase
{
    Task<IOperationResult<ReconcileAccountResult>> HandleAsync(
        ReconcileAccountCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ReconcileAccountHandler : IReconcileAccountUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public ReconcileAccountHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IWorldAccountRepository accounts,
        IClaimRepository claims,
        ILedgerCommit commit,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _accounts = accounts;
        _claims = claims;
        _commit = commit;
        _journal = journal;
    }

    public async Task<IOperationResult<ReconcileAccountResult>> HandleAsync(
        ReconcileAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ReconcileAccountResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<ReconcileAccountResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!AttritionCause.TryNormalize(command.Cause, out var cause))
            return Fail(LedgerErrorCodes.CauseRequired, "Causa de attrition invalida.");

        if (!Guid.TryParse(command.AccountId, out var accountId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta invalida.");

        if (command.ObservedBalance < 0)
            return Fail(LedgerErrorCodes.InvalidAmount, "Saldo observado nao pode ser negativo.");

        var currency = (command.Currency ?? "BRL").Trim().ToUpperInvariant();
        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta nao encontrada.");

        var located = (await _claims.ListAsync(null, accountId, null, cancellationToken)).ToList();
        var active = located
            .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var nexus = account.BalanceOf(currency);
        var observed = Math.Round(command.ObservedBalance, 2, MidpointRounding.AwayFromZero);
        var touched = new List<ClaimAggregate>();

        if (observed == nexus)
        {
            await _commit.SaveAsync([account], [], hop: null, charge: null, cancellationToken);
            _journal.RecordAccountReconciled(account.Id, Guid.Parse(auth.Requester!.AccountId));
            return OperationResult<ReconcileAccountResult>.Success(new ReconcileAccountResult(account.Id, nexus, observed));
        }

        if (observed < nexus)
        {
            var delta = nexus - observed;
            var moved = account.Debit(currency, delta, $"reconcile:{cause}");
            if (moved.IsFailure)
                return OperationResult<ReconcileAccountResult>.Failure(moved.Errors);

            if (!string.IsNullOrWhiteSpace(command.ClaimId))
            {
                if (!Guid.TryParse(command.ClaimId, out var claimId))
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim invalido.");
                var target = active.FirstOrDefault(c => c.Id == claimId);
                if (target is null)
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim ativo nao esta nesta Conta/moeda.");
                if (target.Amount < delta)
                    return Fail(LedgerErrorCodes.HopInvalid, "Claim indicado nao cobre a falta.");
                var adjusted = target.Adjust(target.Amount - delta, currency, accountId);
                if (adjusted.IsFailure)
                    return OperationResult<ReconcileAccountResult>.Failure(adjusted.Errors);
                touched.Add(target);
            }
            else
            {
                if (active.Count == 0)
                    return Fail(LedgerErrorCodes.BundleEmpty, "Sem claims ativos para ratear a falta.");
                var total = active.Sum(c => c.Amount);
                var allocated = 0m;
                for (var i = 0; i < active.Count; i++)
                {
                    var next = i == active.Count - 1
                        ? observed - allocated
                        : Math.Round(active[i].Amount * observed / total, 2, MidpointRounding.AwayFromZero);
                    if (next < 0)
                        next = 0;
                    allocated += next;
                    var adjusted = active[i].Adjust(next, currency, accountId);
                    if (adjusted.IsFailure)
                        return OperationResult<ReconcileAccountResult>.Failure(adjusted.Errors);
                    touched.Add(active[i]);
                }
            }
        }
        else
        {
            var surplus = observed - nexus;
            var credited = account.Credit(currency, surplus, $"reconcile:{cause}");
            if (credited.IsFailure)
                return OperationResult<ReconcileAccountResult>.Failure(credited.Errors);

            var opened = ClaimAggregate.Open(
                OrganizationParty.Id,
                surplus,
                currency,
                Guid.Empty,
                accountId,
                SplitIntent.ResidualOrg);
            if (opened.IsFailure)
                return OperationResult<ReconcileAccountResult>.Failure(opened.Errors);
            touched.Add(opened.Value!);
        }

        var after = located.ToList();
        foreach (var mutated in touched)
        {
            after.RemoveAll(c => c.Id == mutated.Id);
            if (mutated.LocationAccountId == accountId)
                after.Add(mutated);
        }

        var sum = after
            .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Amount);
        if (sum != account.BalanceOf(currency))
            return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos reconciliacao.");

        await _commit.SaveAsync([account], touched, hop: null, charge: null, cancellationToken);
        _journal.RecordAccountReconciled(account.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<ReconcileAccountResult>.Success(
            new ReconcileAccountResult(account.Id, nexus, observed));
    }

    private static IOperationResult<ReconcileAccountResult> Fail(string code, string message) =>
        OperationResult<ReconcileAccountResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

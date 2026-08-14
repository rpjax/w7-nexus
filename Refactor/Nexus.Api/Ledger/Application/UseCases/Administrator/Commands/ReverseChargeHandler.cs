using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record ReverseChargeCommand(string ChargeId, string Cause);
public sealed record ReverseChargeResult(Guid ChargeId, int ReversedClaims);

public interface IReverseChargeUseCase
{
    Task<IOperationResult<ReverseChargeResult>> HandleAsync(
        ReverseChargeCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ReverseChargeHandler : IReverseChargeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IChargeRepository _charges;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public ReverseChargeHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IChargeRepository charges,
        IWorldAccountRepository accounts,
        IClaimRepository claims,
        ILedgerCommit commit,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _charges = charges;
        _accounts = accounts;
        _claims = claims;
        _commit = commit;
        _journal = journal;
    }

    public async Task<IOperationResult<ReverseChargeResult>> HandleAsync(
        ReverseChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ReverseChargeResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<ReverseChargeResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!AttritionCause.TryNormalize(command.Cause, out var cause))
            return Fail(LedgerErrorCodes.CauseRequired, "Estorno exige causa.");

        if (!Guid.TryParse(command.ChargeId, out var chargeId))
            return Fail(LedgerErrorCodes.ChargeNotFound, "Cobrança inválida.");

        var charge = await _charges.GetByIdAsync(chargeId, cancellationToken);
        if (charge is null)
            return Fail(LedgerErrorCodes.ChargeNotFound, "Cobrança nao encontrada.");

        var lineage = await _claims.ListAsync(chargeId, null, null, cancellationToken);
        var active = lineage.Where(c => c.IsActive).ToList();
        var accountsById = new Dictionary<Guid, WorldAccountAggregate>();

        foreach (var group in active.GroupBy(c => c.LocationAccountId))
        {
            var account = await _accounts.GetByIdAsync(group.Key, cancellationToken);
            if (account is null)
                return Fail(LedgerErrorCodes.AccountNotFound, "Conta do claim nao encontrada.");
            accountsById[account.Id] = account;

            foreach (var currencyGroup in group.GroupBy(c => c.Currency, StringComparer.OrdinalIgnoreCase))
            {
                var amount = currencyGroup.Sum(c => c.Amount);
                if (amount <= 0)
                    continue;
                var debited = account.Debit(currencyGroup.Key, amount, "estorno");
                if (debited.IsFailure)
                    return OperationResult<ReverseChargeResult>.Failure(debited.Errors);
            }
        }

        foreach (var claim in active)
        {
            var reversed = claim.Reverse(cause);
            if (reversed.IsFailure)
                return OperationResult<ReverseChargeResult>.Failure(reversed.Errors);
        }

        var marked = charge.MarkReversed();
        if (marked.IsFailure)
            return OperationResult<ReverseChargeResult>.Failure(marked.Errors);

        foreach (var account in accountsById.Values)
        {
            var atAccount = (await _claims.ListAsync(null, account.Id, null, cancellationToken)).ToList();
            foreach (var mutated in active)
            {
                atAccount.RemoveAll(c => c.Id == mutated.Id);
                if (mutated.LocationAccountId == account.Id)
                    atAccount.Add(mutated);
            }

            foreach (var currency in atAccount.Select(c => c.Currency).Concat(account.Balances.Keys)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var sum = atAccount
                    .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
                    .Sum(c => c.Amount);
                if (sum != account.BalanceOf(currency))
                    return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos estorno.");
            }
        }

        await _commit.SaveAsync(accountsById.Values.ToList(), active, hop: null, charge, cancellationToken);
        _journal.RecordChargeReversed(charge.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<ReverseChargeResult>.Success(new ReverseChargeResult(charge.Id, active.Count));
    }

    private static IOperationResult<ReverseChargeResult> Fail(string code, string message) =>
        OperationResult<ReverseChargeResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

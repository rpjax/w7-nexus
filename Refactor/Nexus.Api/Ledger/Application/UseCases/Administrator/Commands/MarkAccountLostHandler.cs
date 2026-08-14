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
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record MarkAccountLostCommand(string AccountId, string Cause);
public sealed record MarkAccountLostResult(Guid AccountId, int WrittenOff);

public interface IMarkAccountLostUseCase
{
    Task<IOperationResult<MarkAccountLostResult>> HandleAsync(
        MarkAccountLostCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MarkAccountLostHandler : IMarkAccountLostUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public MarkAccountLostHandler(
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

    public async Task<IOperationResult<MarkAccountLostResult>> HandleAsync(
        MarkAccountLostCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<MarkAccountLostResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<MarkAccountLostResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!AttritionCause.TryNormalize(command.Cause, out var cause))
            return Fail(LedgerErrorCodes.CauseRequired, "Causa de attrition invalida.");

        if (!Guid.TryParse(command.AccountId, out var accountId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta invalida.");

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta nao encontrada.");

        var located = (await _claims.ListAsync(null, accountId, null, cancellationToken)).ToList();
        var active = located.Where(c => c.IsActive).ToList();
        if (account.BalanceStatus == BalanceStatus.Lost && active.Count == 0 && account.Balances.Values.All(v => v == 0))
        {
            _journal.RecordAccountMarkedLost(account.Id, Guid.Parse(auth.Requester!.AccountId));
            return OperationResult<MarkAccountLostResult>.Success(new MarkAccountLostResult(account.Id, 0));
        }

        var lost = account.SetBalanceStatus(BalanceStatus.Lost);
        if (lost.IsFailure)
            return OperationResult<MarkAccountLostResult>.Failure(lost.Errors);

        foreach (var claim in active)
        {
            var written = claim.WriteOff(cause);
            if (written.IsFailure)
                return OperationResult<MarkAccountLostResult>.Failure(written.Errors);
        }

        foreach (var (currency, amount) in account.Balances.ToList())
        {
            if (amount <= 0)
                continue;
            var debited = account.Debit(currency, amount, $"write-off:{cause}");
            if (debited.IsFailure)
                return OperationResult<MarkAccountLostResult>.Failure(debited.Errors);
        }

        var after = located.ToList();
        foreach (var mutated in active)
        {
            after.RemoveAll(c => c.Id == mutated.Id);
            after.Add(mutated);
        }

        foreach (var currency in account.Balances.Keys.Concat(after.Select(c => c.Currency)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var sum = after
                .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.Amount);
            if (sum != account.BalanceOf(currency))
                return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos write-off.");
        }

        await _commit.SaveAsync([account], active, hop: null, charge: null, cancellationToken);
        _journal.RecordAccountMarkedLost(account.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<MarkAccountLostResult>.Success(new MarkAccountLostResult(account.Id, active.Count));
    }

    private static IOperationResult<MarkAccountLostResult> Fail(string code, string message) =>
        OperationResult<MarkAccountLostResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

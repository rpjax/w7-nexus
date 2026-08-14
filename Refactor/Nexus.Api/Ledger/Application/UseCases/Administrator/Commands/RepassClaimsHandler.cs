using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record RepassClaimsCommand(
    string OriginAccountId,
    IReadOnlyList<string>? ClaimIds,
    string PayoutAccountId);

public sealed record RepassClaimsResult(decimal DebitedAmount, IReadOnlyList<Guid> ClaimIds);

public interface IRepassClaimsUseCase
{
    Task<IOperationResult<RepassClaimsResult>> HandleAsync(
        RepassClaimsCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RepassClaimsHandler : IRepassClaimsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public RepassClaimsHandler(
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

    public async Task<IOperationResult<RepassClaimsResult>> HandleAsync(
        RepassClaimsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RepassClaimsResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<RepassClaimsResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(command.OriginAccountId, out var originId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de origem invalida.");
        if (!Guid.TryParse(command.PayoutAccountId, out var payoutId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de payout invalida.");

        var origin = await _accounts.GetByIdAsync(originId, cancellationToken);
        if (origin is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de origem nao encontrada.");
        if (origin.BalanceStatus == BalanceStatus.Lost)
            return Fail(LedgerErrorCodes.AccountLost, "Conta com saldo perdido nao move.");

        var payout = await _accounts.GetByIdAsync(payoutId, cancellationToken);
        if (payout is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de payout nao encontrada.");
        if (payout.Kind != WorldAccountKind.Payout)
            return Fail(LedgerErrorCodes.NotPayout, "Destino do repasse deve ser Conta Payout.");

        var located = await _claims.ListAsync(null, originId, null, cancellationToken);
        var active = located.Where(c => c.IsActive).ToList();

        List<ClaimAggregate> bundle;
        if (command.ClaimIds is { Count: > 0 })
        {
            bundle = [];
            foreach (var raw in command.ClaimIds)
            {
                if (!Guid.TryParse(raw, out var claimId))
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim invalido.");
                var claim = active.FirstOrDefault(c => c.Id == claimId)
                    ?? await _claims.GetByIdAsync(claimId, cancellationToken);
                if (claim is null)
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim nao encontrado.");
                if (!claim.IsActive || claim.LocationAccountId != originId)
                    return Fail(LedgerErrorCodes.HopInvalid, "Claim fora da origem ou inativo.");
                bundle.Add(claim);
            }
        }
        else
        {
            bundle = active;
        }

        if (bundle.Count == 0)
            return Fail(LedgerErrorCodes.BundleEmpty, "Nenhum claim para repasse.");

        var currencies = bundle.Select(c => c.Currency).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var currency in currencies)
        {
            var amount = bundle.Where(c => string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase)).Sum(c => c.Amount);
            var debited = origin.Debit(currency, amount, "repass");
            if (debited.IsFailure)
                return OperationResult<RepassClaimsResult>.Failure(debited.Errors);
        }

        foreach (var claim in bundle)
        {
            var repassed = claim.Repass();
            if (repassed.IsFailure)
                return OperationResult<RepassClaimsResult>.Failure(repassed.Errors);
        }

        var after = located.ToList();
        foreach (var mutated in bundle)
        {
            after.RemoveAll(c => c.Id == mutated.Id);
            after.Add(mutated);
        }

        foreach (var currency in origin.Balances.Keys.Concat(after.Select(c => c.Currency)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var sum = after
                .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.Amount);
            if (sum != origin.BalanceOf(currency))
                return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos repasse.");
        }

        await _commit.SaveAsync([origin], bundle, hop: null, charge: null, cancellationToken);
        _journal.RecordClaimsRepassed(origin.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<RepassClaimsResult>.Success(
            new RepassClaimsResult(bundle.Sum(c => c.Amount), bundle.Select(c => c.Id).ToList()));
    }

    private static IOperationResult<RepassClaimsResult> Fail(string code, string message) =>
        OperationResult<RepassClaimsResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

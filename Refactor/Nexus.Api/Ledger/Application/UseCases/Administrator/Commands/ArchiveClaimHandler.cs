using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record ArchiveClaimCommand(string ClaimId);
public sealed record ArchiveClaimResult(Guid ClaimId, string Status);

public interface IArchiveClaimUseCase
{
    Task<IOperationResult<ArchiveClaimResult>> HandleAsync(
        ArchiveClaimCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ArchiveClaimHandler : IArchiveClaimUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public ArchiveClaimHandler(
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

    public async Task<IOperationResult<ArchiveClaimResult>> HandleAsync(
        ArchiveClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ArchiveClaimResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<ArchiveClaimResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(command.ClaimId, out var claimId))
            return Fail(LedgerErrorCodes.ClaimNotFound, "Claim invalido.");

        var claim = await _claims.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
            return Fail(LedgerErrorCodes.ClaimNotFound, "Claim nao encontrado.");

        if (claim.Status == ClaimStatus.Archived)
        {
            return OperationResult<ArchiveClaimResult>.Success(
                new ArchiveClaimResult(claim.Id, claim.Status.ToString()));
        }

        var account = await _accounts.GetByIdAsync(claim.LocationAccountId, cancellationToken);
        if (account is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de localizacao nao encontrada.");

        var located = (await _claims.ListAsync(null, account.Id, null, cancellationToken)).ToList();
        var othersActive = located
            .Where(c => c.Id != claim.Id
                && c.IsActive
                && string.Equals(c.Currency, claim.Currency, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Amount);
        var balance = account.BalanceOf(claim.Currency);
        WorldAccountAggregate? touchedAccount = null;

        if (othersActive == balance)
        {
            // Caixa ja bate sem este claim.
        }
        else if (othersActive + claim.Amount == balance && claim.Amount > 0)
        {
            var debited = account.Debit(claim.Currency, claim.Amount, $"archive:{claim.Id}");
            if (debited.IsFailure)
                return OperationResult<ArchiveClaimResult>.Failure(debited.Errors);
            touchedAccount = account;
        }
        else
        {
            return Fail(
                LedgerErrorCodes.UseReconcileEndpoint,
                "Arquivo so casa com o livro se o caixa ja estiver certo ou puder debitar o valor do claim; use reconciliacao.");
        }

        var archived = claim.Archive();
        if (archived.IsFailure)
            return OperationResult<ArchiveClaimResult>.Failure(archived.Errors);

        var after = located.Where(c => c.Id != claim.Id).ToList();
        after.Add(claim);
        var sum = after
            .Where(c => c.IsActive && string.Equals(c.Currency, claim.Currency, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Amount);
        if (sum != account.BalanceOf(claim.Currency))
            return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos arquivo.");

        await _commit.SaveAsync(
            touchedAccount is null ? [] : [touchedAccount],
            [claim],
            hop: null,
            charge: null,
            cancellationToken);
        _journal.RecordClaimArchived(claim.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<ArchiveClaimResult>.Success(
            new ArchiveClaimResult(claim.Id, claim.Status.ToString()));
    }

    private static IOperationResult<ArchiveClaimResult> Fail(string code, string message) =>
        OperationResult<ArchiveClaimResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Errors;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record RevealClaimCommand(string ClaimId, string Summary);
public sealed record RevealClaimResult(Guid ClaimId, bool Visible, decimal ReleasedAmount, string ReleasedCurrency, string Summary);

public interface IRevealClaimUseCase
{
    Task<IOperationResult<RevealClaimResult>> HandleAsync(
        RevealClaimCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RevealClaimHandler : IRevealClaimUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public RevealClaimHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IClaimRepository claims,
        ILedgerCommit commit,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _claims = claims;
        _commit = commit;
        _journal = journal;
    }

    public async Task<IOperationResult<RevealClaimResult>> HandleAsync(
        RevealClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevealClaimResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<RevealClaimResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(command.ClaimId, out var claimId))
            return Fail(LedgerErrorCodes.ClaimNotFound, "Claim invalido.");

        var claim = await _claims.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
            return Fail(LedgerErrorCodes.ClaimNotFound, "Claim nao encontrado.");

        var summary = (command.Summary ?? "").Trim();
        if (!string.Equals(claim.BirthCurrency, claim.Currency, StringComparison.OrdinalIgnoreCase)
            && !summary.Contains(claim.Currency, StringComparison.OrdinalIgnoreCase))
        {
            summary = string.IsNullOrEmpty(summary)
                ? $"Liquidado em {claim.Currency}."
                : $"{summary} Liquidado em {claim.Currency}.";
        }

        var revealed = claim.Reveal(claim.Amount, claim.Currency, summary);
        if (revealed.IsFailure)
            return OperationResult<RevealClaimResult>.Failure(revealed.Errors);

        await _commit.SaveAsync([], [claim], hop: null, charge: null, cancellationToken);
        _journal.RecordClaimRevealed(claim.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<RevealClaimResult>.Success(
            new RevealClaimResult(
                claim.Id,
                claim.Visible,
                claim.ReleasedAmount ?? claim.Amount,
                claim.ReleasedCurrency ?? claim.Currency,
                claim.ReportSummary ?? summary));
    }

    private static IOperationResult<RevealClaimResult> Fail(string code, string message) =>
        OperationResult<RevealClaimResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}

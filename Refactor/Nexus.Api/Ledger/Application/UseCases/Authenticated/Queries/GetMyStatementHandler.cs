using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Authenticated.Queries;

public sealed record StatementLine(
    Guid OriginChargeId,
    string Phase,
    decimal EstimateAmount,
    string EstimateCurrency,
    decimal? ReleasedAmount,
    string? ReleasedCurrency,
    string? Summary,
    string Audience);

public sealed record GetMyStatementQuery;
public sealed record GetMyStatementResult(IReadOnlyList<StatementLine> Items, string View);

public interface IGetMyStatementUseCase
{
    Task<IOperationResult<GetMyStatementResult>> HandleAsync(
        GetMyStatementQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class GetMyStatementHandler : IGetMyStatementUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IClaimRepository _claims;
    private readonly ILedgerAccess _access;
    private readonly IJournalWriter _journal;

    public GetMyStatementHandler(
        IRequestContext requestContext,
        IClaimRepository claims,
        ILedgerAccess access,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _claims = claims;
        _access = access;
        _journal = journal;
    }

    public async Task<IOperationResult<GetMyStatementResult>> HandleAsync(
        GetMyStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        var identity = await LedgerGuards.RequireIdentityAsync<GetMyStatementResult>(_requestContext, cancellationToken);
        if (identity.Failure is not null)
            return identity.Failure;

        var own = (await _claims.ListAsync(null, null, identity.AccountId, cancellationToken))
            .Where(c => c.Kind != ClaimAggregate.PathCutKind || c.BeneficiaryId == identity.AccountId)
            .Select(c => (Claim: c, Audience: "self"))
            .ToList();

        var downlineIds = await _access.ListCarteiraOperatorIdsAsync(identity.AccountId, cancellationToken);
        foreach (var operatorId in downlineIds.Where(id => id != identity.AccountId))
        {
            var slice = await _claims.ListAsync(null, null, operatorId, cancellationToken);
            own.AddRange(slice
                .Where(c => c.Kind != ClaimAggregate.PathCutKind || c.BeneficiaryId == operatorId)
                .Select(c => (Claim: c, Audience: "agency")));
        }

        var lines = own
            .GroupBy(x => x.Claim.OriginChargeId)
            .Select(g => ToLine(g.Key, g.Select(x => x.Claim).ToList(), g.Any(x => x.Audience == "agency") && g.All(x => x.Audience != "self") ? "agency" : "self"))
            .OrderBy(l => l.OriginChargeId)
            .ToList();

        var view = downlineIds.Count > 0 ? "recruiter" : "beneficiary";

        _journal.RecordStatementRead(identity.AccountId);
        return OperationResult<GetMyStatementResult>.Success(new GetMyStatementResult(lines, view));
    }

    internal static StatementLine ToLine(Guid originChargeId, IReadOnlyList<ClaimAggregate> group, string audience)
    {
        var revealed = group.Where(c => c.Visible).ToList();
        var estimate = group.Where(c => c.ParentClaimId is null).ToList();
        if (estimate.Count == 0)
            estimate = group.ToList();

        var estimateAmount = estimate.Sum(c => c.BirthAmount);
        var estimateCurrency = estimate[0].BirthCurrency;
        if (revealed.Count == 0)
        {
            return new StatementLine(
                originChargeId,
                "estimate",
                estimateAmount,
                estimateCurrency,
                null,
                null,
                null,
                audience);
        }

        var released = revealed.Sum(c => c.ReleasedAmount ?? 0);
        var releasedCurrency = revealed[0].ReleasedCurrency;
        var summary = revealed[0].ReportSummary;
        var loss = released <= 0
            || (summary is not null && summary.Contains("perda", StringComparison.OrdinalIgnoreCase));
        return new StatementLine(
            originChargeId,
            loss ? "loss" : "pending",
            estimateAmount,
            estimateCurrency,
            released,
            releasedCurrency,
            summary,
            audience);
    }
}

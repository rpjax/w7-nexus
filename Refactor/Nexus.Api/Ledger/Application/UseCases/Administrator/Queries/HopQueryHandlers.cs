using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Events;
using HopAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop.Hop;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;

public sealed record HopView(
    Guid HopId,
    Guid OriginAccountId,
    string OriginCurrency,
    IReadOnlyList<Guid> BundleClaimIds,
    IReadOnlyList<HopDestinationSnapshot> Destinations,
    Guid? CutOrangeMemberId,
    decimal? CutPercent,
    bool CutInPlace,
    decimal LossAmount,
    DateTime OccurredAt);

public sealed record ListHopsQuery(string? AccountId);
public sealed record ListHopsResult(IReadOnlyList<HopView> Items);

public interface IListHopsUseCase
{
    Task<IOperationResult<ListHopsResult>> HandleAsync(ListHopsQuery query, CancellationToken cancellationToken = default);
}

public sealed class ListHopsHandler : IListHopsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IHopRepository _hops;
    private readonly IJournalWriter _journal;

    public ListHopsHandler(IRequestContext requestContext, ILedgerAccess access, IHopRepository hops, IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _hops = hops;
        _journal = journal;
    }

    public async Task<IOperationResult<ListHopsResult>> HandleAsync(
        ListHopsQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await LedgerGuards.AuthorizeAsync<ListHopsResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        Guid? accountId = Guid.TryParse(query.AccountId, out var parsed) ? parsed : null;
        var items = await _hops.ListAsync(accountId, cancellationToken);
        _journal.RecordHopsListed(Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<ListHopsResult>.Success(new ListHopsResult(items.Select(ToView).ToList()));
    }

    internal static HopView ToView(HopAggregate hop) =>
        new(
            hop.Id,
            hop.OriginAccountId,
            hop.OriginCurrency,
            hop.BundleClaimIds,
            hop.Destinations,
            hop.CutOrangeMemberId,
            hop.CutPercent,
            hop.CutInPlace,
            hop.LossAmount,
            hop.OccurredAt);
}

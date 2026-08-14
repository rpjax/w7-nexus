using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Mandates.Application.Journal;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyCarteira;

public sealed record CarteiraDealView(
    Guid DealId,
    string OperatorAccountId,
    decimal OperatorPercent,
    decimal RecruiterPercent);

public sealed record GetMyCarteiraResult(IReadOnlyList<CarteiraDealView> Items);

public interface IGetMyCarteiraUseCase
{
    Task<IOperationResult<GetMyCarteiraResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class GetMyCarteiraHandler : IGetMyCarteiraUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAgencyDealReadRepository _dealReadRepository;
    private readonly IJournalWriter _journal;

    public GetMyCarteiraHandler(
        IRequestContext requestContext,
        IAgencyDealReadRepository dealReadRepository,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _dealReadRepository = dealReadRepository;
        _journal = journal;
    }

    public async Task<IOperationResult<GetMyCarteiraResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<GetMyCarteiraResult>.Failure(requesterResult.Errors);

        var recruiterId = new MemberId(Guid.Parse(requester.AccountId));
        var deals = await _dealReadRepository.ListActiveByRecruiterAsync(recruiterId, cancellationToken);
        _journal.RecordCarteiraRead(recruiterId.Value);
        return OperationResult<GetMyCarteiraResult>.Success(new GetMyCarteiraResult(
            deals.Select(d => new CarteiraDealView(
                d.Id,
                d.OperatorId.ToString(),
                d.OperatorPercent,
                d.RecruiterPercent)).ToList()));
    }
}

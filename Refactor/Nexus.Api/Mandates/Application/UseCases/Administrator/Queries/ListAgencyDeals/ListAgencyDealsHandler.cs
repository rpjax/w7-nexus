using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListAgencyDeals;

public sealed record AgencyDealView(
    Guid DealId,
    string RecruiterAccountId,
    string OperatorAccountId,
    decimal OperatorPercent,
    decimal RecruiterPercent,
    string Status);

public sealed record ListAgencyDealsResult(IReadOnlyList<AgencyDealView> Items);

public interface IListAgencyDealsUseCase
{
    Task<IOperationResult<ListAgencyDealsResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class ListAgencyDealsHandler : IListAgencyDealsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAgencyDealReadRepository _dealReadRepository;

    public ListAgencyDealsHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAgencyDealReadRepository dealReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _dealReadRepository = dealReadRepository;
    }

    public async Task<IOperationResult<ListAgencyDealsResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<ListAgencyDealsResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        var deals = await _dealReadRepository.ListActiveAsync(cancellationToken);
        return OperationResult<ListAgencyDealsResult>.Success(new ListAgencyDealsResult(
            deals.Select(d => new AgencyDealView(
                d.Id,
                d.RecruiterId.ToString(),
                d.OperatorId.ToString(),
                d.OperatorPercent,
                d.RecruiterPercent,
                d.Status.ToString())).ToList()));
    }
}

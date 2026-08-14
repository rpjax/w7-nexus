using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListShareholders;

public sealed record ShareholderView(string AccountId, decimal Percentage);

public sealed record ListShareholdersResult(IReadOnlyList<ShareholderView> Items, decimal TotalPercent);

public interface IListShareholdersUseCase
{
    Task<IOperationResult<ListShareholdersResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class ListShareholdersHandler : IListShareholdersUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IShareholderStakeReadRepository _stakeReadRepository;

    public ListShareholdersHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IShareholderStakeReadRepository stakeReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _stakeReadRepository = stakeReadRepository;
    }

    public async Task<IOperationResult<ListShareholdersResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<ListShareholdersResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        var stakes = await _stakeReadRepository.ListAllAsync(cancellationToken);
        return OperationResult<ListShareholdersResult>.Success(new ListShareholdersResult(
            stakes.Select(s => new ShareholderView(s.AccountId.ToString(), s.Percentage)).ToList(),
            stakes.Sum(s => s.Percentage)));
    }
}

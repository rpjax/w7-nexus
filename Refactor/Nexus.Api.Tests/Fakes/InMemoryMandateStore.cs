using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;

namespace Refactor.Nexus.Api.Tests.Fakes;

internal sealed class InMemoryAccountDirectory : IAccountDirectory
{
    private readonly HashSet<Guid> _accounts = [];
    private readonly HashSet<Guid> _admins = [];

    public void Add(Account account)
    {
        _accounts.Add(account.Id.Value);
        if (account.IsAdministrator)
            _admins.Add(account.Id.Value);
    }

    public void Add(MemberId id, bool isAdministrator = false)
    {
        _accounts.Add(id.Value);
        if (isAdministrator)
            _admins.Add(id.Value);
    }

    public Task<bool> ExistsAsync(MemberId accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.Contains(accountId.Value));

    public Task<bool> IsAdministratorAsync(MemberId accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_admins.Contains(accountId.Value));
}

internal sealed class InMemoryMandateStore :
    IMemberMandateRepository,
    IMemberMandateReadRepository,
    IAgencyDealRepository,
    IAgencyDealReadRepository,
    IShareholderStakeRepository,
    IShareholderStakeReadRepository
{
    private readonly Dictionary<Guid, MemberMandate> _mandates = [];
    private readonly Dictionary<Guid, AgencyDealAggregate> _deals = [];
    private readonly Dictionary<Guid, ShareholderStakeAggregate> _stakes = [];

    public Task<MemberMandate?> GetByMemberIdAsync(MemberId memberId, CancellationToken cancellationToken = default)
    {
        _mandates.TryGetValue(memberId.Value, out var mandate);
        return Task.FromResult(mandate);
    }

    public Task SaveAsync(MemberMandate mandate, CancellationToken cancellationToken = default)
    {
        _mandates[mandate.MemberId.Value] = MemberMandate.Rehydrate(
            mandate.MemberId,
            mandate.Grants.ToList(),
            mandate.AppliedPresets.ToList());
        mandate.ClearUncommitted();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MemberMandate>> ListGrantedByAsync(MemberId grantorId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MemberMandate>>(
            _mandates.Values.Where(m => m.Grants.Any(g => g.GrantedBy.Equals(grantorId))).ToList());

    public Task<IReadOnlyList<MemberMandate>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MemberMandate>>(_mandates.Values.ToList());

    public Task<AgencyDealAggregate?> GetActiveByOperatorIdAsync(MemberId operatorId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_deals.Values.FirstOrDefault(d => d.IsActive && d.OperatorId.Equals(operatorId)));

    public Task<AgencyDealAggregate?> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        _deals.TryGetValue(dealId, out var deal);
        return Task.FromResult(deal);
    }

    public Task SaveAsync(AgencyDealAggregate deal, CancellationToken cancellationToken = default)
    {
        _deals[deal.Id] = deal;
        deal.ClearUncommitted();
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveDealForOperatorAsync(MemberId operatorId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_deals.Values.Any(d => d.IsActive && d.OperatorId.Equals(operatorId)));

    public Task<IReadOnlyList<AgencyDealAggregate>> ListActiveByRecruiterAsync(MemberId recruiterId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AgencyDealAggregate>>(
            _deals.Values.Where(d => d.IsActive && d.RecruiterId.Equals(recruiterId)).ToList());

    public Task<IReadOnlyList<AgencyDealAggregate>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AgencyDealAggregate>>(_deals.Values.Where(d => d.IsActive).ToList());

    public Task<ShareholderStakeAggregate?> GetByAccountIdAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        _stakes.TryGetValue(accountId.Value, out var stake);
        return Task.FromResult(stake);
    }

    public Task SaveAsync(ShareholderStakeAggregate stake, CancellationToken cancellationToken = default)
    {
        _stakes[stake.AccountId.Value] = stake;
        stake.ClearUncommitted();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        _stakes.Remove(accountId.Value);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<ShareholderStakeAggregate>> IShareholderStakeReadRepository.ListAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ShareholderStakeAggregate>>(_stakes.Values.ToList());

    public Task<decimal> SumPercentagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_stakes.Values.Sum(s => s.Percentage));
}

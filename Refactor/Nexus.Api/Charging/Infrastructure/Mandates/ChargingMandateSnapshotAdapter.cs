using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Charging.Infrastructure.Mandates;

public sealed class ChargingMandateSnapshotAdapter : IChargingMandateSnapshot
{
    private readonly IAccountDirectory _accounts;
    private readonly IMemberMandateReadRepository _mandates;
    private readonly IAgencyDealReadRepository _deals;
    private readonly IShareholderStakeReadRepository _stakes;

    public ChargingMandateSnapshotAdapter(
        IAccountDirectory accounts,
        IMemberMandateReadRepository mandates,
        IAgencyDealReadRepository deals,
        IShareholderStakeReadRepository stakes)
    {
        _accounts = accounts;
        _mandates = mandates;
        _deals = deals;
        _stakes = stakes;
    }

    public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _accounts.IsAdministratorAsync(new MemberId(accountId), cancellationToken);

    public async Task<bool> IsEligibleOrangeAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var memberId = new MemberId(accountId);
        if (!await _accounts.ExistsAsync(memberId, cancellationToken))
            return false;

        var mandate = await _mandates.GetByMemberIdAsync(memberId, cancellationToken);
        if (mandate is null)
            return false;

        return mandate.AppliedPresets.Contains(PresetIds.Orange, StringComparer.OrdinalIgnoreCase)
            || mandate.HasCapability(Capabilities.AtuarComoLaranja, MandateScope.Organization());
    }

    public async Task<MandateChargingSnapshot?> CaptureAsync(Guid operatorMemberId, CancellationToken cancellationToken = default)
    {
        var deal = await _deals.GetActiveByOperatorIdAsync(new MemberId(operatorMemberId), cancellationToken);
        if (deal is null)
            return null;

        var stakes = await _stakes.ListAllAsync(cancellationToken);
        return new MandateChargingSnapshot(
            new AgencySlice(deal.OperatorId.Value, deal.OperatorPercent, deal.RecruiterId.Value, deal.RecruiterPercent),
            stakes.Select(s => new ShareholderSlice(s.AccountId.Value, s.Percentage)).ToList());
    }
}

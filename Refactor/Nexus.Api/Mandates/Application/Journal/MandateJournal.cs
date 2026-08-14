using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;

namespace Refactor.Nexus.Api.Mandates.Application.Journal;

internal static class MandateJournal
{
    public static void RecordPresetGranted(this IJournalWriter journal, MemberId accountId, string presetId, MemberId grantedBy)
    {
        journal.Append(new MandatePresetGranted
        {
            AccountId = accountId.Value,
            PresetId = presetId,
            GrantedBy = grantedBy.Value
        });
    }

    public static void RecordPresetRevoked(this IJournalWriter journal, MemberId accountId, string presetId)
    {
        journal.Append(new MandatePresetRevoked
        {
            AccountId = accountId.Value,
            PresetId = presetId
        });
    }

    public static void RecordCapabilityGranted(
        this IJournalWriter journal,
        MemberId accountId,
        string capability,
        string scopeKind)
    {
        journal.Append(new MandateCapabilityGranted
        {
            AccountId = accountId.Value,
            Capability = capability,
            ScopeKind = scopeKind
        });
    }

    public static void RecordCapabilityRevoked(
        this IJournalWriter journal,
        MemberId accountId,
        string capability,
        string scopeKind)
    {
        journal.Append(new MandateCapabilityRevoked
        {
            AccountId = accountId.Value,
            Capability = capability,
            ScopeKind = scopeKind
        });
    }

    public static void RecordDealUpserted(this IJournalWriter journal, AgencyDealAggregate deal)
    {
        journal.Append(new AgencyDealUpserted
        {
            DealId = deal.Id,
            OperatorId = deal.OperatorId.Value,
            RecruiterId = deal.RecruiterId.Value,
            OperatorPercent = deal.OperatorPercent,
            RecruiterPercent = deal.RecruiterPercent
        });
    }

    public static void RecordDealClosed(this IJournalWriter journal, AgencyDealAggregate deal)
    {
        journal.Append(new AgencyDealClosed
        {
            DealId = deal.Id,
            OperatorId = deal.OperatorId.Value
        });
    }

    public static void RecordStakeChanged(
        this IJournalWriter journal,
        ShareholderStakeAggregate stake,
        string change)
    {
        journal.Append(new ShareholderStakeChanged
        {
            AccountId = stake.AccountId.Value,
            Percentage = stake.Percentage,
            Change = change
        });
    }

    public static void RecordStakeRemoved(this IJournalWriter journal, MemberId accountId)
    {
        journal.Append(new ShareholderStakeChanged
        {
            AccountId = accountId.Value,
            Percentage = 0,
            Change = "removed"
        });
    }
}

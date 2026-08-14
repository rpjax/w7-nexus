using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Ledger.Application.Journal;

internal static class LedgerJournal
{
    public static void RecordStatementRead(this IJournalWriter journal, Guid memberId) =>
        journal.Append(new LedgerStatementRead { MemberId = memberId });

    public static void RecordClaimRevealed(this IJournalWriter journal, Guid claimId, Guid actedBy) =>
        journal.Append(new LedgerClaimRevealed { ClaimId = claimId, ActedBy = actedBy });

    public static void RecordChargeMaterialized(this IJournalWriter journal, Guid chargeId, Guid actedBy) =>
        journal.Append(new LedgerChargeMaterialized { ChargeId = chargeId, ActedBy = actedBy });

    public static void RecordHopRegistered(this IJournalWriter journal, Guid hopId, Guid actedBy) =>
        journal.Append(new LedgerHopRegistered { HopId = hopId, ActedBy = actedBy });

    public static void RecordClaimsRepassed(this IJournalWriter journal, Guid originAccountId, Guid actedBy) =>
        journal.Append(new LedgerClaimsRepassed { OriginAccountId = originAccountId, ActedBy = actedBy });

    public static void RecordAccountMarkedLost(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new LedgerAccountMarkedLost { AccountId = accountId, ActedBy = actedBy });

    public static void RecordAccountReconciled(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new LedgerAccountReconciled { AccountId = accountId, ActedBy = actedBy });

    public static void RecordChargeReversed(this IJournalWriter journal, Guid chargeId, Guid actedBy) =>
        journal.Append(new LedgerChargeReversed { ChargeId = chargeId, ActedBy = actedBy });

    public static void RecordClaimArchived(this IJournalWriter journal, Guid claimId, Guid actedBy) =>
        journal.Append(new LedgerClaimArchived { ClaimId = claimId, ActedBy = actedBy });

    public static void RecordClaimsListed(this IJournalWriter journal, Guid actedBy) =>
        journal.Append(new LedgerClaimsListed { ActedBy = actedBy });

    public static void RecordClaimRead(this IJournalWriter journal, Guid claimId, Guid actedBy) =>
        journal.Append(new LedgerClaimRead { ClaimId = claimId, ActedBy = actedBy });

    public static void RecordHopsListed(this IJournalWriter journal, Guid actedBy) =>
        journal.Append(new LedgerHopsListed { ActedBy = actedBy });
}

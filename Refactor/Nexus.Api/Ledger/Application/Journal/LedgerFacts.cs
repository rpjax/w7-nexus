using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Ledger.Application.Journal;

[CanonicalFact("Ledger.StatementRead", schemaVersion: 1, Owner = "ledger", Name = "Statement read")]
public sealed class LedgerStatementRead
{
    [JournalIndex("member")]
    public required Guid MemberId { get; init; }
}

[CanonicalFact("Ledger.ClaimRevealed", schemaVersion: 1, Owner = "ledger", Name = "Claim revealed")]
public sealed class LedgerClaimRevealed
{
    [JournalIndex("claim")]
    public required Guid ClaimId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ChargeMaterialized", schemaVersion: 1, Owner = "ledger", Name = "Charge materialized")]
public sealed class LedgerChargeMaterialized
{
    [JournalIndex("charge")]
    public required Guid ChargeId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.HopRegistered", schemaVersion: 1, Owner = "ledger", Name = "Hop registered")]
public sealed class LedgerHopRegistered
{
    [JournalIndex("hop")]
    public required Guid HopId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ClaimsRepassed", schemaVersion: 1, Owner = "ledger", Name = "Claims repassed")]
public sealed class LedgerClaimsRepassed
{
    [JournalIndex("account")]
    public required Guid OriginAccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.AccountMarkedLost", schemaVersion: 1, Owner = "ledger", Name = "Account marked lost")]
public sealed class LedgerAccountMarkedLost
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.AccountReconciled", schemaVersion: 1, Owner = "ledger", Name = "Account reconciled")]
public sealed class LedgerAccountReconciled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ChargeReversed", schemaVersion: 1, Owner = "ledger", Name = "Charge reversed")]
public sealed class LedgerChargeReversed
{
    [JournalIndex("charge")]
    public required Guid ChargeId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ClaimArchived", schemaVersion: 1, Owner = "ledger", Name = "Claim archived")]
public sealed class LedgerClaimArchived
{
    [JournalIndex("claim")]
    public required Guid ClaimId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ClaimsListed", schemaVersion: 1, Owner = "ledger", Name = "Claims listed")]
public sealed class LedgerClaimsListed
{
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.ClaimRead", schemaVersion: 1, Owner = "ledger", Name = "Claim read")]
public sealed class LedgerClaimRead
{
    [JournalIndex("claim")]
    public required Guid ClaimId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Ledger.HopsListed", schemaVersion: 1, Owner = "ledger", Name = "Hops listed")]
public sealed class LedgerHopsListed
{
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

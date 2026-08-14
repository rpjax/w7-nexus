using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.WorldAccounts.Application.Journal;

[CanonicalFact("WorldAccounts.Opened", schemaVersion: 1, Owner = "world-accounts", Name = "World account opened")]
public sealed class WorldAccountOpened
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("WorldAccounts.Labeled", schemaVersion: 1, Owner = "world-accounts", Name = "World account labeled")]
public sealed class WorldAccountLabeled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("WorldAccounts.Configured", schemaVersion: 1, Owner = "world-accounts", Name = "World account configured")]
public sealed class WorldAccountConfigured
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("WorldAccounts.ObservationRecorded", schemaVersion: 1, Owner = "world-accounts", Name = "Observation recorded")]
public sealed class WorldAccountObservationRecorded
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("WorldAccounts.Listed", schemaVersion: 1, Owner = "world-accounts", Name = "World accounts listed")]
public sealed class WorldAccountsListed
{
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("WorldAccounts.Read", schemaVersion: 1, Owner = "world-accounts", Name = "World account read")]
public sealed class WorldAccountRead
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

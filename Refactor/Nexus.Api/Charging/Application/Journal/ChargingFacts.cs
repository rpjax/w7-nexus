using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Charging.Application.Journal;

[CanonicalFact("Charging.ChargeCreated", schemaVersion: 1, Owner = "charging", Name = "Charge created")]
public sealed class ChargingChargeCreated
{
    [JournalIndex("charge")]
    public required Guid ChargeId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Charging.ChargeTransitioned", schemaVersion: 1, Owner = "charging", Name = "Charge transitioned")]
public sealed class ChargingChargeTransitioned
{
    [JournalIndex("charge")]
    public required Guid ChargeId { get; init; }
    [JournalIndex("member")]
    public Guid? ActedBy { get; init; }
}

[CanonicalFact("Charging.RailBound", schemaVersion: 1, Owner = "charging", Name = "Emission rail bound")]
public sealed class ChargingRailBound
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Charging.RailUnbound", schemaVersion: 1, Owner = "charging", Name = "Emission rail unbound")]
public sealed class ChargingRailUnbound
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

[CanonicalFact("Charging.RailsListed", schemaVersion: 1, Owner = "charging", Name = "Emission rails listed")]
public sealed class ChargingRailsListed
{
    [JournalIndex("member")]
    public required Guid ActedBy { get; init; }
}

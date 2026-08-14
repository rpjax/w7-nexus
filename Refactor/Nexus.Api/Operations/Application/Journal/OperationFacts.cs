using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Operations.Application.Journal;

[CanonicalFact("Operations.OperationCreated", schemaVersion: 1, Owner = "operations", Name = "Operation created")]
public sealed class OperationCreated
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    public required string OperationKey { get; init; }
    public required string Name { get; init; }
}

[CanonicalFact("Operations.OperationTransitioned", schemaVersion: 1, Owner = "operations", Name = "Operation transitioned")]
public sealed class OperationTransitioned
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    public required string FromStatus { get; init; }
    public required string ToStatus { get; init; }
}

[CanonicalFact("Operations.OperatorAssigned", schemaVersion: 1, Owner = "operations", Name = "Operator assigned")]
public sealed class OperatorAssigned
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    [JournalIndex("member")]
    public required Guid MemberId { get; init; }
}

[CanonicalFact("Operations.OperatorUnassigned", schemaVersion: 1, Owner = "operations", Name = "Operator unassigned")]
public sealed class OperatorUnassigned
{
    [JournalIndex("operation")]
    public required Guid OperationId { get; init; }
    [JournalIndex("member")]
    public required Guid MemberId { get; init; }
}

[CanonicalFact("Operations.ScriptRegistered", schemaVersion: 1, Owner = "operations", Name = "Script registered")]
public sealed class ScriptRegistered
{
    [JournalIndex("script")]
    public required Guid ScriptId { get; init; }
    public required string OperationKey { get; init; }
}

[CanonicalFact("Operations.StoreObjectUpserted", schemaVersion: 1, Owner = "operations", Name = "Store object upserted")]
public sealed class StoreObjectUpserted
{
    [JournalIndex("store_object")]
    public required Guid ObjectId { get; init; }
    public required string OperationKey { get; init; }
}

[CanonicalFact("Operations.StoreObjectDeleted", schemaVersion: 1, Owner = "operations", Name = "Store object deleted")]
public sealed class StoreObjectDeleted
{
    [JournalIndex("store_object")]
    public required Guid ObjectId { get; init; }
    public required string OperationKey { get; init; }
}
